using Microsoft.EntityFrameworkCore;
using QueueManagement.Api;
using QueueManagement.Api.Middleware;
using QueueManagement.Application;
using QueueManagement.Infrastructure;
using QueueManagement.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


try
{
    builder.Services
        .AddApi()
        .AddInfrastructure(builder.Configuration)
        .AddApplication();

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}