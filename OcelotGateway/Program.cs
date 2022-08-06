using ApiGateway;
using Serilog;
using Serilog.Debugging;

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
            .AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", true, true);

            Log.Logger = new LoggerConfiguration().ReadFrom
                .Configuration(configurationBuilder.Build())
                .Destructure.ToMaximumDepth(5).CreateLogger();

            Log.Information("Application starting...");

            CreateHostBuilder(args,  configurationBuilder).Build().Run();
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