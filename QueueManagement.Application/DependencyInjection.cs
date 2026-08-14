using Microsoft.Extensions.DependencyInjection;
using QueueManagement.Application.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.Validators.Auth;


namespace QueueManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters()
            .AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        services.AddScoped<IQueueService, QueueService>();
        
        return services;
    }
}