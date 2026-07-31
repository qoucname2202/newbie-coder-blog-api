using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class LevelServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly LevelRepository _levelRepo;
    private readonly TestAuditLogService _auditLog;
    private readonly LevelService _sut;

    public LevelServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _levelRepo = new LevelRepository(_db);
        _auditLog = new TestAuditLogService();
        _sut = new LevelService(_levelRepo, _auditLog);
    }

    public void Dispose() => _db.Dispose();

    #region Seed helpers

    private async Task<Level> SeedLevelAsync(
        long id = 1,
        string code = "ENTRY",
        string name = "Entry",
        string? description = null,
        int displayOrder = 0,
        bool isActive = true,
        bool isDeleted = false,
        DateTimeOffset? deletedAt = null,
        long? deletedBy = null)
    {
        var level = new Level
        {
            Id = id,
            Code = code,
            Name = name,
            Description = description,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            EffDate = DateTimeOffset.UtcNow.AddDays(-10),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-5)
        };

        if (isDeleted)
        {
            level.DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
            level.DeletedBy = deletedBy;
            level.IsActive = false;
        }

        _db.Levels.Add(level);
        await _db.SaveChangesAsync();
        return level;
    }

    private async Task<InterviewQuestion> SeedInterviewQuestionAsync(
        long id = 1,
        long levelId = 1,
        long authorId = 99)
    {
        var question = new InterviewQuestion
        {
            Id = id,
            Title = "Test Question",
            Slug = "test-question",
            QuestionContent = "Test content.",
            Level = InterviewLevel.Middle,
            LevelId = levelId,
            Status = PostStatus.Draft,
            AuthorId = authorId,
            EffDate = DateTimeOffset.UtcNow.AddDays(-5),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-1)
        };
        _db.InterviewQuestions.Add(question);
        await _db.SaveChangesAsync();
        return question;
    }

    #endregion

    #region CreateLevelAsync

    [Fact]
    public async Task CreateLevel_WithValidRequest_ReturnsCreatedLevel()
    {
        // Arrange
        var request = new CreateLevelRequest
        {
            Code = "SENIOR",
            Name = "Senior",
            Description = "Senior developer level",
            DisplayOrder = 3,
            IsActive = true
        };

        // Act
        var result = await _sut.CreateLevelAsync(
            request,
            createdByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-1");

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("SENIOR", result.Code);
        Assert.Equal("Senior", result.Name);
        Assert.Equal("Senior developer level", result.Description);
        Assert.Equal(3, result.DisplayOrder);
        Assert.True(result.IsActive);

        // Verify DB
        var dbLevel = await _db.Levels.FindAsync(result.Id);
        Assert.NotNull(dbLevel);
        Assert.Equal("SENIOR", dbLevel.Code);
    }

    [Fact]
    public async Task CreateLevel_WithWhitespaceCode_ThrowsBusinessException()
    {
        // Arrange
        var request = new CreateLevelRequest
        {
            Code = "   ",
            Name = "Entry"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateLevelAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelCodeRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateLevel_WithWhitespaceName_ThrowsBusinessException()
    {
        // Arrange
        var request = new CreateLevelRequest
        {
            Code = "ENTRY",
            Name = "   "
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateLevelAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelNameRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateLevel_WithDuplicateCode_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(code: "ENTRY", name: "Entry");

        var request = new CreateLevelRequest { Code = "ENTRY", Name = "Entry Level" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateLevelAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelCodeAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateLevel_WithDuplicateName_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(code: "ENTRY", name: "Entry");

        var request = new CreateLevelRequest { Code = "ENTRY_2", Name = "Entry" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateLevelAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelNameAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateLevel_WithCaseInsensitiveDuplicateCode_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(code: "JUNIOR", name: "Junior");

        var request = new CreateLevelRequest { Code = "junior", Name = "Junior Dev" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateLevelAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelCodeAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateLevel_WritesAuditLog()
    {
        // Arrange
        var request = new CreateLevelRequest { Code = "EXPERT", Name = "Expert" };

        // Act
        await _sut.CreateLevelAsync(
            request,
            createdByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-2");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.LevelCreated, _auditLog.LoggedAction);
        Assert.Equal(nameof(Level), _auditLog.LoggedEntityType);
        Assert.NotNull(_auditLog.LoggedNewValue);
    }

    #endregion

    #region GetLevelsAsync

    [Fact]
    public async Task GetLevels_ReturnsPagedResults()
    {
        // Arrange
        await SeedLevelAsync(1, "A", "Alpha");
        await SeedLevelAsync(2, "B", "Beta");
        await SeedLevelAsync(3, "C", "Gamma");

        var filter = new LevelFilterRequest { Page = 1, PageSize = 2 };

        // Act
        var result = await _sut.GetLevelsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetLevels_WithKeyword_FiltersResults()
    {
        // Arrange
        await SeedLevelAsync(1, "ENTRY", "Entry Level");
        await SeedLevelAsync(2, "JUNIOR", "Junior Level");
        await SeedLevelAsync(3, "SENIOR", "Senior Level");

        var filter = new LevelFilterRequest { Keyword = "junior" };

        // Act
        var result = await _sut.GetLevelsAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Junior Level", result.Items[0].Name);
    }

    [Fact]
    public async Task GetLevels_WithIsActiveFilter_FiltersResults()
    {
        // Arrange
        await SeedLevelAsync(1, "A", "Active Level", isActive: true);
        await SeedLevelAsync(2, "I", "Inactive Level", isActive: false);

        var filter = new LevelFilterRequest { IsActive = true };

        // Act
        var result = await _sut.GetLevelsAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsActive);
    }

    [Fact]
    public async Task GetLevels_ExcludesSoftDeletedLevels()
    {
        // Arrange
        await SeedLevelAsync(1, "A", "Active Level");
        await SeedLevelAsync(2, "D", "Deleted Level", isDeleted: true);

        var filter = new LevelFilterRequest();

        // Act
        var result = await _sut.GetLevelsAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Active Level", result.Items[0].Name);
    }

    [Fact]
    public async Task GetLevels_Pagination_Works()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
            await SeedLevelAsync(i, $"L{i}", $"Level {i}");

        var filter = new LevelFilterRequest { Page = 2, PageSize = 2 };

        // Act
        var result = await _sut.GetLevelsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    #endregion

    #region GetLevelByIdAsync

    [Fact]
    public async Task GetLevelById_WithValidId_ReturnsDetail()
    {
        // Arrange
        await SeedLevelAsync(1, "MIDDLE", "Middle", "Middle developer", displayOrder: 2, isActive: true);

        // Act
        var result = await _sut.GetLevelByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("MIDDLE", result.Code);
        Assert.Equal("Middle", result.Name);
        Assert.Equal("Middle developer", result.Description);
        Assert.Equal(2, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetLevelById_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.GetLevelByIdAsync(9999));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelNotFound, ex.ResponseCode);
    }

    #endregion

    #region UpdateLevelAsync

    [Fact]
    public async Task UpdateLevel_WithValidRequest_UpdatesLevel()
    {
        // Arrange
        await SeedLevelAsync(1, "MIDDLE", "Middle", "Old description", displayOrder: 1);

        var request = new UpdateLevelRequest
        {
            Code = "SENIOR",
            Name = "Senior",
            Description = "New description",
            DisplayOrder = 3,
            IsActive = true
        };

        // Act
        var result = await _sut.UpdateLevelAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-3");

        // Assert
        Assert.Equal("SENIOR", result.Code);
        Assert.Equal("Senior", result.Name);
        Assert.Equal("New description", result.Description);
        Assert.Equal(3, result.DisplayOrder);
    }

    [Fact]
    public async Task UpdateLevel_WithWhitespaceCode_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(1);

        var request = new UpdateLevelRequest { Code = "   ", Name = "Entry" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateLevelAsync(1, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelCodeRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateLevel_WithWhitespaceName_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(1);

        var request = new UpdateLevelRequest { Code = "ENTRY", Name = "   " };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateLevelAsync(1, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelNameRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateLevel_WithDuplicateCode_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(1, "A", "Alpha");
        await SeedLevelAsync(2, "B", "Beta");

        var request = new UpdateLevelRequest { Code = "A", Name = "Beta Updated" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateLevelAsync(2, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelCodeAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateLevel_WithDuplicateName_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(1, "A", "Alpha");
        await SeedLevelAsync(2, "B", "Beta");

        var request = new UpdateLevelRequest { Code = "B", Name = "Alpha" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateLevelAsync(2, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelNameAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateLevel_WithNonExistentId_ThrowsBusinessException()
    {
        // Arrange
        var request = new UpdateLevelRequest { Code = "ENTRY", Name = "Entry" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateLevelAsync(9999, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.LevelNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateLevel_WritesAuditLog()
    {
        // Arrange
        await SeedLevelAsync(1);

        var request = new UpdateLevelRequest { Code = "ENTRY", Name = "Entry Updated" };

        // Act
        await _sut.UpdateLevelAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-4");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.LevelUpdated, _auditLog.LoggedAction);
        Assert.NotNull(_auditLog.LoggedOldValue);
        Assert.NotNull(_auditLog.LoggedNewValue);
    }

    #endregion

    #region DeleteLevelAsync

    [Fact]
    public async Task DeleteLevel_SoftDeletesLevel()
    {
        // Arrange
        await SeedLevelAsync(1, "CUSTOM", "Custom Level");

        // Act
        await _sut.DeleteLevelAsync(
            1,
            deletedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-5");

        // Assert
        var dbLevel = await _db.Levels.FindAsync(1L);
        Assert.NotNull(dbLevel!.DeletedAt);
        Assert.Equal(99, dbLevel.DeletedBy);
        Assert.False(dbLevel.IsActive);
    }

    [Fact]
    public async Task DeleteLevel_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteLevelAsync(9999, deletedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteLevel_WithAssociatedInterviewQuestions_ThrowsBusinessException()
    {
        // Arrange
        await SeedLevelAsync(1);
        await SeedInterviewQuestionAsync(levelId: 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteLevelAsync(1, deletedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelHasInterviewQuestions, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteLevel_WritesAuditLog()
    {
        // Arrange
        await SeedLevelAsync(1);

        // Act
        await _sut.DeleteLevelAsync(
            1,
            deletedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-6");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.LevelDeleted, _auditLog.LoggedAction);
    }

    #endregion

    #region RestoreLevelAsync

    [Fact]
    public async Task RestoreLevel_RestoresDeletedLevel()
    {
        // Arrange
        await SeedLevelAsync(1, "CUSTOM", "Custom Level", isDeleted: true);

        // Act
        var result = await _sut.RestoreLevelAsync(
            1,
            restoredByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-7");

        // Assert
        var dbLevel = await _db.Levels.FindAsync(1L);
        Assert.Null(dbLevel!.DeletedAt);
        Assert.Null(dbLevel.DeletedBy);
        Assert.True(dbLevel.IsActive);
    }

    [Fact]
    public async Task RestoreLevel_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestoreLevelAsync(9999, restoredByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.LevelNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task RestoreLevel_WritesAuditLog()
    {
        // Arrange
        await SeedLevelAsync(1, isDeleted: true);

        // Act
        await _sut.RestoreLevelAsync(
            1,
            restoredByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-8");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.LevelRestored, _auditLog.LoggedAction);
    }

    #endregion
}
