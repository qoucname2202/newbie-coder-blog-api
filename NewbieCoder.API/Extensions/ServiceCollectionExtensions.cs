using Microsoft.OpenApi.Models;
using NewbieCoder.Core.Constants;
using NewbieCoder.Infrastructure;

namespace NewbieCoder.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        

        services.AddHttpContextAccessor();
        services.AddApiRateLimiting(configuration);
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(AppConstants.ApiVersion, new OpenApiInfo
            {
                Title = AppConstants.ApiTitle,
                Version = AppConstants.ApiVersion
            });
        });

        return services;
    }
}
