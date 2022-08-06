using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Winton.Extensions.Configuration.Consul;

namespace Infrastructure.Extensions
{
    public static class ConsulExtensions
    {
        public static IServiceCollection AddConsulService(this IServiceCollection services, IConfiguration configuration)
        {
            var consulConfig = configuration.GetSection("Consul").Get<ConsulConfiguration>();
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulClientConfig =>
            {
                consulClientConfig.Address = new Uri(consulConfig.Host);
                consulClientConfig.WaitTime = TimeSpan.FromSeconds(Convert.ToDouble(consulConfig.WaitTime == 0 ? 5 : consulConfig.WaitTime));
                consulClientConfig.Token = consulConfig.Token;
            }));
            return services;
        }

        public static IApplicationBuilder UseConsulRegister(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()?.CreateLogger("ConsulExtensions");
            var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();

            var consulConfig = configuration.GetSection("Consul").Get<ConsulConfiguration>();

            var appUrl = configuration.GetValue<string>("APPLICATION_URL");

            if (string.IsNullOrWhiteSpace(appUrl))
            {
                logger?.LogInformation("UseConsulRegister -> ApplicationUrl is empty.Unable to register with the consul.");
                return app;
            }
            logger?.LogInformation("UseConsulRegister -> ApplicationUrl:{@applicationUrl}", appUrl);

            var address = new Uri(appUrl);
            if (address.Host.Contains("localhost"))
            {
                logger.LogInformation("Consul does not active because application is running on the localhost.");
                return app;
            }
            var applicationName = configuration.GetValue<string>("ApplicationName");

            var registration = new AgentServiceRegistration()
            {
                ID = $"{applicationName}-{address.Host}:{address.Port}",
                Name = $"{applicationName}",
                Address = $"{address.Host}",
                Port = address.Port,
            };
            
            registration.Checks = consulConfig.AgentCheckRegistrations
                .Select(t => new Consul.AgentCheckRegistration()
                {
                    HTTP = new Uri(address, t.Endpoint).OriginalString,
                    Notes = t.Notes,
                    Timeout = TimeSpan.FromSeconds(t.Timeout),
                    Interval = TimeSpan.FromSeconds(t.Interval),
                    TLSSkipVerify = t.TLSSkipVerify,
                    Method = t.HttpMethod
                }).ToArray();

            logger?.LogInformation("UseConsulRegister -> Registering to Consul -> Id:{@Id}", registration.ID);
            consulClient.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
            consulClient.Agent.ServiceRegister(registration).GetAwaiter().GetResult();

            lifetime.ApplicationStopping.Register(() =>
            {
                logger?.LogInformation("UseConsulRegister -> Unregistering from Consul -> Id:{@Id}", registration.ID);
                consulClient.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
            });

            return app;
        }
        public static Action<IConsulConfigurationSource> ConsulConfigurationSourceAction(TimeSpan poolWaitTime, TimeSpan onWatchExceptionTime)
        {
            var consulEndpoint = Environment.GetEnvironmentVariable("CONSUL_ENDPOINT");
            if (string.IsNullOrWhiteSpace(consulEndpoint))
                throw new Exception("Consul endpoint must not empty");
            return (options) =>
            {
                options.ConsulConfigurationOptions = consulConfOptions =>
                {
                    consulConfOptions.Address = new Uri(consulEndpoint);
                };
                options.Optional = true;
                options.PollWaitTime = poolWaitTime == default ? TimeSpan.FromSeconds(5) : poolWaitTime;
                options.ReloadOnChange = true;
                options.OnLoadException = exceptionContext =>
                {
                    exceptionContext.Ignore = true;
                };
                options.OnWatchException = exceptionContext =>
                {
                    return onWatchExceptionTime == default ? TimeSpan.FromSeconds(5) : onWatchExceptionTime;
                };
            };
        }
    }
}
