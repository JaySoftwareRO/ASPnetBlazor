using System.Net;
using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ui_agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Loopback, 19872, opts =>
                            {
                                opts.UseHttps("ssl/localhost.pfx");
                            });

                            options.Listen(IPAddress.Loopback, 19871);
                        })
                        .UseStaticWebAssets()
                        .UseStartup<Startup>()
                        .UseElectron(args);
                });
    }
}
