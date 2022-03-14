using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost
    .UseKestrel()
    .UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config
            .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddOcelot();
        services.AddHealthChecks();
    }).
    ConfigureLogging((hostingContext, logging) =>
    {
        //add your logging
        logging.AddSimpleConsole();
    })
    .Configure(app =>
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        name = app.ApplicationServices.GetService<IConfiguration>().GetValue<string>("APPLICATION_NAME"),
                        status = report.Status.ToString(),
                        components = report.Entries.Select(entry => new { key = entry.Key, value = entry.Value.Status.ToString() })
                    }, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
                    await context.Response.WriteAsync(result);
                }
            });
        });
        app.UseOcelot();
    });

var app = builder.Build();

app.Run();
