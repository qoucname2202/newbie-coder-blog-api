using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.Image;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.Options;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Implementation of image storage operations using Cloudinary.
/// </summary>
public sealed class ImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ImageUploadOptions _options;
    private readonly ILogger<ImageStorageService> _logger;

    private static readonly Dictionary<string, byte[]> ImageSignatures = new()
    {
        { "jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { "jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { "png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        { "webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } }
    };

    public ImageStorageService(
        IOptions<CloudinaryOptions> cloudinaryOptions,
        IOptions<ImageUploadOptions> imageUploadOptions,
        ILogger<ImageStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(cloudinaryOptions.Value);
        ArgumentNullException.ThrowIfNull(imageUploadOptions.Value);

        var cloudinarySettings = cloudinaryOptions.Value;

        if (string.IsNullOrWhiteSpace(cloudinarySettings.CloudName))
        {
            throw new ImageUploadException(
                ResponseMessages.CloudinaryNotConfigured,
                ResponseCodes.ValidationError,
                HttpStatusCodes.InternalServerError);
        }

        Account account = new(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret);

        _cloudinary = new Cloudinary(account);
        _options = imageUploadOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImageUploadResponse> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        await using var stream = file.OpenReadStream();
        var imageInfo = await GetImageInfoAsync(stream, cancellationToken);

        ValidateDimensions(imageInfo.Width, imageInfo.Height);
        ValidateFileSignature(file.FileName, stream);

        var uploadParams = BuildUploadParams(imageInfo.Width, imageInfo.Height);
        var uploadResult = await UploadToCloudinaryAsync(stream, file.FileName, uploadParams, cancellationToken);

        return MapToResponse(uploadResult);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ImageUploadResponse>> UploadManyAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            throw new ImageUploadException(
                ResponseMessages.ImageFileEmpty,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        if (files.Count > _options.MaxFileCount)
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageTooManyFiles, _options.MaxFileCount),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        var results = new List<ImageUploadResponse>();

        foreach (var file in files)
        {
            try
            {
                var result = await UploadAsync(file, cancellationToken);
                results.Add(result);
            }
            catch (ImageUploadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image: {FileName}", file.FileName);
                throw new ImageUploadException(
                    ResponseMessages.ImageUploadFailed,
                    ex,
                    ResponseCodes.InternalError,
                    HttpStatusCodes.InternalServerError);
            }
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ImageUploadException(
                ResponseMessages.ImagePublicIdRequired,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.NotFound)
            {
                _logger.LogError("Cloudinary delete failed with status {StatusCode} for PublicId: {PublicId}",
                    result.StatusCode, publicId);
                throw new ImageUploadException(
                    ResponseMessages.ImageDeleteFailed,
                    ResponseCodes.InternalError,
                    HttpStatusCodes.InternalServerError);
            }
        }
        catch (ImageUploadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image with PublicId: {PublicId}", publicId);
            throw new ImageUploadException(
                ResponseMessages.ImageDeleteFailed,
                ex,
                ResponseCodes.InternalError,
                HttpStatusCodes.InternalServerError);
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ImageUploadException(
                ResponseMessages.ImageFileEmpty,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageFileTooLarge, FormatBytes(_options.MaxFileSizeBytes)),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) ||
            !_options.AllowedExtensions.Contains(extension))
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageInvalidExtension, string.Join(", ", _options.AllowedExtensions)),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        if (!_options.AllowedMimeTypes.Contains(file.ContentType))
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageInvalidMimeType, string.Join(", ", _options.AllowedMimeTypes)),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }
    }

    private async Task<ImageInfo> GetImageInfoAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            stream.Position = 0;
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var bytes = memoryStream.ToArray();

            if (bytes.Length < 24)
            {
                throw new ImageUploadException(
                    ResponseMessages.ImageInvalidFormat,
                    ResponseCodes.ValidationError,
                    HttpStatusCodes.BadRequest);
            }

            int width, height;

            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                (width, height) = GetJpegDimensions(bytes);
            }
            else if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                (width, height) = GetPngDimensions(bytes);
            }
            else if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                     bytes.Length >= 12 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            {
                (width, height) = GetWebpDimensions(bytes);
            }
            else
            {
                throw new ImageUploadException(
                    ResponseMessages.ImageInvalidFormat,
                    ResponseCodes.ValidationError,
                    HttpStatusCodes.BadRequest);
            }

            if (width <= 0 || height <= 0)
            {
                throw new ImageUploadException(
                    ResponseMessages.ImageInvalidDimensions,
                    ResponseCodes.ValidationError,
                    HttpStatusCodes.BadRequest);
            }

            return new ImageInfo(width, height);
        }
        catch (ImageUploadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read image dimensions");
            throw new ImageUploadException(
                ResponseMessages.ImageUnreadable,
                ex,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }
    }

    private static (int Width, int Height) GetJpegDimensions(byte[] bytes)
    {
        int i = 2;
        while (i < bytes.Length - 8)
        {
            if (bytes[i] != 0xFF) break;
            var marker = bytes[i + 1];
            if (marker == 0xC0 || marker == 0xC2)
            {
                var height = (bytes[i + 5] << 8) | bytes[i + 6];
                var width = (bytes[i + 7] << 8) | bytes[i + 8];
                return (width, height);
            }
            var length = (bytes[i + 2] << 8) | bytes[i + 3];
            i += 2 + length;
        }
        return (0, 0);
    }

    private static (int Width, int Height) GetPngDimensions(byte[] bytes)
    {
        if (bytes.Length < 24) return (0, 0);
        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (width, height);
    }

    private static (int Width, int Height) GetWebpDimensions(byte[] bytes)
    {
        if (bytes.Length < 30) return (0, 0);
        if (bytes[11] == 0x4C) // Lossy WebP
        {
            var width = (bytes[26] | (bytes[27] << 8)) & 0x3FFF;
            var height = (bytes[28] | (bytes[29] << 8)) & 0x3FFF;
            return (width, height);
        }
        if (bytes[11] == 0x4B) // Lossless WebP
        {
            var width = (bytes[22] | (bytes[23] << 8) | (bytes[24] << 16) | (bytes[25] << 24)) & 0x3FFF;
            var height = (bytes[26] | (bytes[27] << 8) | (bytes[28] << 16) | (bytes[29] << 24)) & 0x3FFF;
            return (width, height);
        }
        return (0, 0);
    }

    private void ValidateDimensions(int width, int height)
    {
        if (_options.MaxWidth.HasValue && width > _options.MaxWidth.Value)
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageWidthExceeded, _options.MaxWidth.Value, width),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        if (_options.MaxHeight.HasValue && height > _options.MaxHeight.Value)
        {
            throw new ImageUploadException(
                string.Format(ResponseMessages.ImageHeightExceeded, _options.MaxHeight.Value, height),
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }
    }

    private void ValidateFileSignature(string fileName, Stream stream)
    {
        stream.Position = 0;
        var buffer = new byte[12];
        var bytesRead = stream.Read(buffer, 0, 12);

        if (bytesRead < 4)
        {
            throw new ImageUploadException(
                ResponseMessages.ImageInvalidFormat,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant().TrimStart('.');

        if (string.IsNullOrEmpty(extension))
        {
            throw new ImageUploadException(
                ResponseMessages.ImageInvalidExtension,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        if (!ImageSignatures.TryGetValue(extension, out var expectedSignature))
        {
            throw new ImageUploadException(
                ResponseMessages.ImageInvalidFormat,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        bool isValid;
        if (extension == "webp")
        {
            isValid = buffer.Take(expectedSignature.Length).SequenceEqual(expectedSignature) &&
                      bytesRead >= 12 &&
                      buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50;
        }
        else
        {
            isValid = buffer.Take(expectedSignature.Length).SequenceEqual(expectedSignature);
        }

        if (!isValid)
        {
            throw new ImageUploadException(
                ResponseMessages.ImageInvalidFormat,
                ResponseCodes.ValidationError,
                HttpStatusCodes.BadRequest);
        }

        stream.Position = 0;
    }

    private string GeneratePublicId()
    {
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString("N");
        return $"{_options.CloudinaryFolder}/{now:yyyy/MM}/{guid}";
    }

    private ImageUploadParams BuildUploadParams(int width, int height)
    {
        return new ImageUploadParams
        {
            File = new FileDescription(Guid.NewGuid().ToString()),
            Folder = _options.CloudinaryFolder,
            PublicId = GeneratePublicId(),
            Overwrite = false,
            UseFilename = false,
            UniqueFilename = true,
            Transformation = BuildTransformation(width, height)
        };
    }

    private Transformation BuildTransformation(int width, int height)
    {
        var transformation = new Transformation();

        if (_options.AutoOptimize)
        {
            transformation.Quality("auto");
            transformation.FetchFormat("auto");
        }
        else if (_options.ImageQuality.HasValue)
        {
            transformation.Quality(_options.ImageQuality.Value);
        }

        return transformation;
    }

    private async Task<ImageUploadResult> UploadToCloudinaryAsync(
        Stream stream,
        string fileName,
        ImageUploadParams uploadParams,
        CancellationToken cancellationToken)
    {
        try
        {
            stream.Position = 0;
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            uploadParams.File = new FileDescription(fileName, memoryStream);

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.StatusCode != HttpStatusCode.OK && uploadResult.StatusCode != HttpStatusCode.Created)
            {
                _logger.LogError("Cloudinary upload failed with status {StatusCode}: {Error}",
                    uploadResult.StatusCode, uploadResult.Error?.Message);
                throw new ImageUploadException(
                    ResponseMessages.ImageUploadFailed,
                    ResponseCodes.InternalError,
                    HttpStatusCodes.InternalServerError);
            }

            return uploadResult;
        }
        catch (ImageUploadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Cloudinary: {FileName}", fileName);
            throw new ImageUploadException(
                ResponseMessages.ImageUploadFailed,
                ex,
                ResponseCodes.InternalError,
                HttpStatusCodes.InternalServerError);
        }
    }

    private static ImageUploadResponse MapToResponse(ImageUploadResult result)
    {
        return new ImageUploadResponse
        {
            PublicId = result.PublicId ?? string.Empty,
            SecureUrl = result.SecureUrl?.ToString() ?? string.Empty,
            Format = result.Format ?? string.Empty,
            Bytes = result.Bytes,
            Width = result.Width,
            Height = result.Height
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private sealed record ImageInfo(int Width, int Height);
}
