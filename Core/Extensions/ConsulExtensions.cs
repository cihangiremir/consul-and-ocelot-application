using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Winton.Extensions.Configuration.Consul;

namespace Core.Extensions
{
    public static class ConsulExtensions
    {
        public static IServiceCollection AddConsulService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = configuration.GetSection("Consul:Host").Value;
                consulConfig.Address = new Uri(address);
            }));
            return services;
        }

        public static IApplicationBuilder UseConsulRegister(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()?.CreateLogger("ConsulExtensions");
            var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();

            var appUrl = configuration.GetSection("APPLICATION_URL").Value;
            logger?.LogInformation("UseConsulRegister -> ApplicationUrl:{@applicationUrl}", appUrl);
            if (string.IsNullOrEmpty(appUrl))
            {
                logger?.LogInformation("UseConsulRegister -> ApplicationUrl is empty.Unable to register with the consul.");
                return app;
            }
            var address = new Uri(appUrl);

            var registration = new AgentServiceRegistration()
            {
                ID = $"{configuration.GetSection("ApplicationName").Value}-{address.Host}:{address.Port}",
                Name = $"{configuration.GetSection("ApplicationName").Value}",
                Address = $"{address.Host}",
                Port = address.Port,
            };
            if (!registration.Address.Contains("localhost"))
            {
                registration.Checks = new AgentCheckRegistration[]
                {
                    new AgentCheckRegistration
                    {
                        HTTP = new Uri(address,"health").OriginalString,
                        Notes = "Checks ReportService /health",
                        Timeout = TimeSpan.FromSeconds(5) ,
                        Interval = TimeSpan.FromSeconds(20),
                        TLSSkipVerify=true,
                        Method="GET"
                    }
                };
            }
            logger?.LogInformation("UseConsulRegister -> Registering to Consul -> Id:{@Id}", registration.ID);
            consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(false);
            consulClient.Agent.ServiceRegister(registration).ConfigureAwait(false);

            lifetime.ApplicationStopping.Register(() =>
            {
                logger?.LogInformation("UseConsulRegister -> Unregistering from Consul -> Id:{@Id}", registration.ID);
                consulClient.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
            });

            return app;
        }
        public static Action<IConsulConfigurationSource> ConsulConfigurationSourceAction()
        {
            var consulEndpoint = Environment.GetEnvironmentVariable("CONSUL_ENDPOINT");
            if (string.IsNullOrEmpty(consulEndpoint))
                throw new Exception("Consul endpoint must not empty");
            return (options) =>
            {
                options.ConsulConfigurationOptions = consulConfOptions =>
                {
                    consulConfOptions.Address = new Uri(consulEndpoint);
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
            };
        }
    }
}
