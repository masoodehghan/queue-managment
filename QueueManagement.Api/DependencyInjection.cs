using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using QueueManagement.Api.Filters;

namespace QueueManagement.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "My API",
                Version = "v1"
            });
            
            options.AddSecurityDefinition(
                "identity-password",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,

                    Flows = new OpenApiOAuthFlows
                    {
                        Password = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri(configuration["SwaggerSettings:TokenUrl"]),

                            Scopes = new Dictionary<string, string>()
                        }
                    }
                });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "identity-password"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddControllers(options => { options.Filters.Add<ValidationFilter>(); });

        services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

        return services;
    }
}
