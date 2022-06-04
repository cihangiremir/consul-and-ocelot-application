using Core.Extensions;
using Serilog;
using Serilog.Debugging;
using UserApi;
using Winton.Extensions.Configuration.Consul;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            SelfLog.Enable(Console.Error);
            var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddConsul($"UserApi/appsettings.{aspNetCoreEnvironment}.json", ConsulExtensions.ConsulConfigurationSourceAction());

            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configurationBuilder.Build())
                .Destructure.ToMaximumDepth(5).CreateLogger();

            Log.Information("Starting app");

            CreateHostBuilder(args, aspNetCoreEnvironment).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    public static IHostBuilder CreateHostBuilder(string[] args, string aspNetCoreEnvironment)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                      .AddEnvironmentVariables();
                if (args is not null) config.AddCommandLine(args);

                config.AddConsul($"UserApi/appsettings.{aspNetCoreEnvironment}.json", ConsulExtensions.ConsulConfigurationSourceAction());
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseIISIntegration()
                .UseKestrel()
                .UseStartup<Startup>();
            });
    }
}