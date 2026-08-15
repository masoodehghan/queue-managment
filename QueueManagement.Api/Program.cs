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
        .AddApi(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddApplication();

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            c.OAuthClientId("swagger");
            c.OAuthAppName("Swagger UI");
            c.OAuthUsePkce();
        });
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