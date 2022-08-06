using Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace UserApi
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
            services.AddControllers();

            services.AddHealthChecks();

            services.AddConsulService(Configuration);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen();

        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline.

            app.UseSwagger();

            app.UseSwaggerUI();

            app.UseRouting();

            app.UseAuthorization();

            app.UseConsulRegister();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = async (c, r) =>
                    {
                        c.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new
                        {
                            name = Configuration.GetValue<string>("ApplicationName"),
                            status = r.Status.ToString(),
                            components = r.Entries.Select(entry => new { key = entry.Key, value = entry.Value.Status.ToString() })
                        }, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
                        await c.Response.WriteAsync(result);
                    }
                });
            });
        }
    }
}
