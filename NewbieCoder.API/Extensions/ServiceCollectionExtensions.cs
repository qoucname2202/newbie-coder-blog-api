using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewbieCoder.API.Authorization;
using NewbieCoder.API.Middlewares;
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
        services.AddControllers();
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

            // --- Tags ordering ---
            options.OrderActionsBy(o => o.RelativePath);
        });

        return services;
    }
}
