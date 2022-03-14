using ProductApi;
using Serilog;
using Serilog.Debugging;
using Winton.Extensions.Configuration.Consul;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            SelfLog.Enable(Console.Error);

            var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddConsul($"ProductApi/appsettings{(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Contains("Development") ? ".Development" : "")}.json", options =>
            {
                options.ConsulConfigurationOptions = consulConfOptions =>
                {
                    consulConfOptions.Address = new Uri(Environment.GetEnvironmentVariable("CONSUL_ENDPOINT"));
                };
                options.Optional = true;
                options.PollWaitTime = TimeSpan.FromSeconds(5);
                options.ReloadOnChange = true;
                options.OnLoadException = exceptionContext =>
                {
                    Log.Error("Consul OnLoadException -> Exception:{@ex}", exceptionContext.Exception);
                    exceptionContext.Ignore = true;
                };
                options.OnWatchException = exceptionContext =>
                {
                    Log.Error("Consul OnWatchException -> Exception:{@ex}", exceptionContext.Exception);
                    return TimeSpan.FromSeconds(2);
                };
            });

            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configurationBuilder.Build())
                .Destructure.ToMaximumDepth(5).CreateLogger();

            Log.Debug($"Logger created.");
            Log.Information("Starting app");

            CreateWebHostBuilder(args).Build().Run();
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
    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return new WebHostBuilder()
            .UseKestrel(options =>
            {
                options.AllowSynchronousIO = true;
            })
            .UseSerilog()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                      .AddEnvironmentVariables();
                if (args is not null) config.AddCommandLine(args);

                config.AddConsul($"ProductApi/appsettings{(hostingContext.HostingEnvironment.IsDevelopment() ? ".Development" : "")}.json", options =>
                     {
                         options.ConsulConfigurationOptions = consulConfOptions =>
                         {
                             consulConfOptions.Address = new Uri(Environment.GetEnvironmentVariable("CONSUL_ENDPOINT"));
                         };
                         options.Optional = true;
                         options.PollWaitTime = TimeSpan.FromSeconds(5);
                         options.ReloadOnChange = true;
                         options.OnLoadException = exceptionContext =>
                         {
                             Log.Error("Consul OnLoadException -> Exception:{@ex}", exceptionContext.Exception);
                             exceptionContext.Ignore = true;
                         };
                         options.OnWatchException = exceptionContext =>
                         {
                             Log.Error("Consul OnWatchException -> Exception:{@ex}", exceptionContext.Exception);
                             return TimeSpan.FromSeconds(2);
                         };
                     });
            })
            .UseIISIntegration()
            .UseStartup<Startup>();
    }
}