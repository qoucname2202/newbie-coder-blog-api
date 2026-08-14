using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewbieCoder.API.Authorization;
using NewbieCoder.API.Converters;
using NewbieCoder.API.Middlewares;
using NewbieCoder.API.Options;
using NewbieCoder.API.Validators;
using NewbieCoder.Core.Constants;
using NewbieCoder.Infrastructure;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        services.AddHttpContextAccessor();
        services.AddApiRateLimiting(configuration);

        // Swagger Basic Auth — only register the validator in non-Testing environments.
        // In Testing environment the .env file is absent; Swagger Basic Auth middleware
        // is also skipped in the pipeline, so credentials are never needed.
        if (!string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Testing",
                StringComparison.OrdinalIgnoreCase))
        {
            services.ConfigureOptions<SwaggerAuthOptionsValidator>();
        }

        // JWT settings — read from environment variables (loaded from .env via DotNetEnv).
        var jwtSettings = new JwtSettings
        {
            Secret = Environment.GetEnvironmentVariable("JwtSettings__Secret")
                ?? configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT Secret is required (set JwtSettings__Secret in .env or JwtSettings:Secret in appsettings.json)"),
            Issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer")
                ?? configuration["JwtSettings:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is required (set JwtSettings__Issuer in .env or JwtSettings:Issuer in appsettings.json)"),
            Audience = Environment.GetEnvironmentVariable("JwtSettings__Audience")
                ?? configuration["JwtSettings:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is required (set JwtSettings__Audience in .env or JwtSettings:Audience in appsettings.json)")
        };
        services.AddSingleton(jwtSettings);

        // JwtMiddlewareSettings used by AuthMiddleware.
        var authMiddlewareSettings = new JwtMiddlewareSettings
        {
            Secret = jwtSettings.Secret,
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience
        };
        services.AddSingleton(authMiddlewareSettings);

        // ASP.NET Core JWT Bearer authentication — validates tokens for HttpContext.User.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options => options.AddApiAuthorization());
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new StrictJsonConverterFactory());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var trace = context.HttpContext.GetRequestTrace();

                    var allErrors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors.Select(err => new { Key = e.Key, Message = err.ErrorMessage }))
                        .Where(x => !string.IsNullOrWhiteSpace(x.Message))
                        .ToList();

                    var allErrorMessages = allErrors.Select(e => e.Message).ToList();

                    // Count required field errors from empty body {}
                    var requiredFieldErrors = allErrorMessages
                        .Where(m => m.Contains("field is required", StringComparison.OrdinalIgnoreCase) ||
                                    m.Contains("is required", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    string responseData;

                    // If 3+ required field errors → empty body case, list all required fields
                    if (requiredFieldErrors.Count >= 3)
                    {
                        var fieldNames = allErrors
                            .Where(e => !string.IsNullOrWhiteSpace(e.Key) &&
                                        !e.Key.Equals("$", StringComparison.OrdinalIgnoreCase))
                            .Select(e => char.ToLowerInvariant(e.Key[0]) + e.Key[1..])
                            .Distinct()
                            .ToList();

                        responseData = $"The following fields are required: {string.Join(", ", fieldNames)}.";
                    }
                    else
                    {
                        // Priority 1: Type conversion errors on specific fields (not root $).
                        // Excludes root-level errors to avoid "request field is required" alongside type error.
                        var typeErrorField = allErrors
                            .FirstOrDefault(e =>
                                e.Key != "$" &&
                                (e.Message.Contains("could not be converted to", StringComparison.OrdinalIgnoreCase) ||
                                 e.Message.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase)));

                        if (typeErrorField != null)
                        {
                            responseData = typeErrorField.Key.ToLowerInvariant() switch
                            {
                                "password" => ResponseMessages.PasswordInvalidFormat,
                                "remember_me" => ResponseMessages.RememberMeInvalidFormat,
                                _ => ResponseMessages.LoginIdInvalidFormat
                            };
                        }
                        else
                        {
                            // Priority 2: Skip framework technical messages, prefer custom FluentValidation messages.
                            // Special case: when a field has BOTH [TrimmedRequired] and [MinLength] errors (e.g. empty/whitespace-only
                            // input fails TrimmedRequired but may also fail MinLength), prefer the TrimmedRequired message
                            // to avoid showing redundant "required" + "min length" pairs.
                            responseData = allErrorMessages
                                .FirstOrDefault(m =>
                                    !m.Contains("field is required", StringComparison.OrdinalIgnoreCase) &&
                                    !m.Contains("request field is required", StringComparison.OrdinalIgnoreCase) &&
                                    !m.Contains("could not be converted to", StringComparison.OrdinalIgnoreCase) &&
                                    !m.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase) &&
                                    !m.Contains("is required", StringComparison.OrdinalIgnoreCase))
                                ?? allErrorMessages
                                    .FirstOrDefault(m =>
                                        m.Contains("vui lòng", StringComparison.OrdinalIgnoreCase) ||
                                        m.Contains("please enter", StringComparison.OrdinalIgnoreCase))
                                ?? allErrorMessages
                                    .FirstOrDefault(m =>
                                        m.Contains("at least", StringComparison.OrdinalIgnoreCase) ||
                                        m.Contains("ít nhất", StringComparison.OrdinalIgnoreCase) ||
                                        m.Contains("must be at least", StringComparison.OrdinalIgnoreCase))
                                ?? ResponseMessages.ValidationError;
                        }
                    }

                    var response = new
                    {
                        RequestTrace = trace,
                        ResponseDateTime = DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                        ResponseData = responseData,
                        ResponseStatus = new
                        {
                            ResponseCode = ResponseCodes.ValidationError,
                            ResponseMessage = ResponseMessages.ValidationError,
                            TracingMessage = (string?)null
                        }
                    };

                    context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.HttpContext.Response.ContentType = "application/json";
                    return new JsonResult(response) { StatusCode = 400 };
                };
            });
        services.AddValidatorsFromAssemblyContaining<CreateLevelRequestValidator>();
        services.AddFluentValidationAutoValidation();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(AppConstants.ApiVersion, new OpenApiInfo
            {
                Title = AppConstants.ApiTitle,
                Version = AppConstants.ApiVersion,
                Description = AppConstants.ApiDescription,
                TermsOfService = new Uri(AppConstants.ApiTermsOfService),
                Contact = new OpenApiContact
                {
                    Name = AppConstants.ApiContactName,
                    Email = AppConstants.ApiContactEmail
                },
                License = new OpenApiLicense
                {
                    Name = AppConstants.ApiLicenseName,
                    Url = new Uri(AppConstants.ApiLicenseUrl)
                }
            });

            // Load XML comments so <summary> doc comments render in Swagger UI.
            var xmlFile = $"{typeof(AppConstants).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            // --- JWT Bearer ---
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // --- HTTP Basic for Swagger ---
            options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Description = "HTTP Basic Authentication for Swagger UI. Enter username and password.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "basic"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Basic"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // --- Tags ordering ---
            options.OrderActionsBy(o => o.RelativePath);
        });

        return services;
    }
}
