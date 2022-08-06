using Infrastructure.Extensions;
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
            .AddEnvironmentVariables()
            .AddConsul($"UserApi/appsettings.{aspNetCoreEnvironment}.json", ConsulExtensions.ConsulConfigurationSourceAction(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5)));
            if (args is not null) configurationBuilder.AddCommandLine(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configurationBuilder.Build())
                .Destructure.ToMaximumDepth(5)
                .CreateLogger();

            Log.Information("Application starting...");

            CreateHostBuilder(args, configurationBuilder).Build().Run();
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
    public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationBuilder configurationBuilder)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((config) =>
            {
                config = configurationBuilder;
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel().UseStartup<Startup>();
            });
    }
}