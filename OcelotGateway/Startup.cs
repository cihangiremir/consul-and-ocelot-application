using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using System.Text.Json;
using Infrastructure.Extensions;

namespace ApiGateway
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            services.AddOcelot().AddConsul();

            services.AddConsulService(Configuration);

        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseConsulRegister();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = async (c, r) =>
                    {
                        c.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new
                        {
                            name = Configuration.GetValue<string>("ApplicationName"),
                            status = r.Status.ToString(),
                            components = r.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() })
                        }, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
                        await c.Response.WriteAsync(result);
                    }
                });
            });

            app.UseOcelot();
        }
    }
}
