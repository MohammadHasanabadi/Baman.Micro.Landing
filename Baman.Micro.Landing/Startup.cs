using Manex.CoreLib.Formatters.JsonNet;
using Manex.CoreLib.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Baman.Micro.Landing.Data;
using Microsoft.AspNetCore.Mvc.Versioning;
using Autofac;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Baman.Micro.Landing
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            configuration.UseInManex(o => {
                o.ServiceName = "landing";
                o.Doc.PublishBasePath = $"{o.ServiceName}";//v2/
                o.Doc.XmlFilePath = Path.Combine(AppContext.BaseDirectory, GetType().Namespace + ".xml");
                o.UseFormatter<JsonNetFormatterProvider>(f => {
                    f.AcceptInputLongString = true;
                    f.OutputLongAsString = true;
                });
                o.Logging.DisableRequestResponseLogging = configuration.GetValue<bool>("Logging:DisableRequestResponseLogging");
                o.UseJwt2Auth(Configuration["AuthUrl"]);
                o.ConfigureCacheOptions = options => {
                    options.Password = "bmnRDS20";
                    options.EndPoints.AddRange(new[] {
                        "10.1.10.150:6379",
                        "10.1.10.155:6379",
                        "10.1.10.160:6379"
                    });
                };

                var healthCheckOptions = new ManexHealthCheckOptions();
                configuration.GetSection("ManexHealthCheckOptions")
                             .Bind(healthCheckOptions);
                o.HealthCheckOptions = healthCheckOptions;
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNpgsqlPool<EntityContext>("DefaultConnection");

            services.AddSingleton(Configuration);
            services.AddMemoryCache();
            services.AddManex();
            services.Configure<ApiCallUrlOptions>(options => Configuration.GetSection("ApiCallUrlOptions").Bind(options));
            services.AddApiVersioning(o => {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.ReportApiVersions = true;
                //o.ApiVersionReader = new QueryStringApiVersionReader("ver", "api-version");
                o.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("mnx-apiversion"));
            });
            services.AddBamanHealthCheck();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.ConfigureManex(GetType().Assembly, typeof(EntityContext).Assembly);

        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IApiVersionDescriptionProvider provider)
        {
            loggerFactory.AddManexLoggerProvider();

            //app.UseDeveloperExceptionPage();

            if (env.IsDevelopment() || env.IsDev() || env.IsTest())
                app.UseDeveloperExceptionPage();
            else if (env.IsProduction() || env.IsStaging())
            {
                //app.UseManexRequestResponseHandler();
                app.UseManexExceptionHandler();
            }

            // if (env.IsDevelopment())
            //     app.UseDeveloperExceptionPage();
            // else if (env.IsProduction() || env.IsStaging()) app.UseManexExceptionHandler();

            app.UseManex(provider);

            app.UseEndpoints(endpoint => {
                endpoint.MapBamanHealthChecks();
            });
        }
    }
}
