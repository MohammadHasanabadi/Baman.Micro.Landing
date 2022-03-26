using Autofac;
using Baman.Micro.Landing.Data;
using Baman.Micro.Report.Core.Messaging.Publish;
using Manex.CoreLib.Hosting;
using Manex.CoreLib.Messaging;
using Manex.CoreLib.Scheduling;
using Manex.CoreLib.Scheduling.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Baman.Micro.Landing.Daemons
{
    public class Program
    {
        static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => BamanHost.CreateDaemon("Baman.Micro.Report.Daemons", args,
                                      postAddDefaultAppSettings: (env, config) => {
                                          config.AddJsonFile("appsettings.workers.json", false, true);
                                          config.AddJsonFile("appsettings.messaging.json", false, true);
                                          config.AddJsonFile($"appsettings.workers.{env.EnvironmentName}.json", false,
                                              true);
                                          config.AddJsonFile($"appsettings.messaging.{env.EnvironmentName}.json", false,
                                              true);
                                      })
                        .ConfigureServices((hostContext, services) => {
                            var cs = hostContext.Configuration.GetConnectionString("DefaultConnection");
                            services.AddNpgsqlPool<EntityContext>(cs);
                            services.AddManex();

                            services.AddOptions();

                            services.Configure<ApplicationOptions>(options => {
                                options.AppAssemblies = new[] { typeof(Program).Assembly };
                            });

                            services.Configure<WorkerRootOptions>(o
                                                                      => hostContext.Configuration
                                                                         .GetSection("WorkerOptions")
                                                                         .Bind(o));

                            services.Configure<MessagingOptions>(o
                                                                     => hostContext.Configuration
                                                                        .GetSection("MessagingOptions")
                                                                        .Bind(o));

                            services.AddMessaging(typeof(EntityContext).Assembly, typeof(Program).Assembly);
                            services.AddHostedWorker(typeof(EntityContext).Assembly, typeof(Program).Assembly);
                        })
                        .ConfigureContainer<ContainerBuilder>(builder => {
                            // builder.ConfigureManex(typeof(Program).Assembly, typeof(EntityContext).Assembly);
                            builder.Register(builder => {
                                var options = builder.Resolve<IOptions<MessagingOptions>>();
                                return MessagePublisher.CreateInstance(options);
                            }).As<IMessagePublisher>().SingleInstance();

                        });
}
}
