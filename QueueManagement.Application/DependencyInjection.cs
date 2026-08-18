using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.Services;
using QueueManagement.Application.Validators.Auth;

namespace QueueManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
        services.AddScoped<IQueueService, QueueService>();

        return services;
    }
}
