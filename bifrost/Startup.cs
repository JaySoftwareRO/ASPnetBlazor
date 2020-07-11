using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace bifrost
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = this.Configuration.GetValue<string>("Data:PSQLCache:ConnectionString");

            services
                .AddCaches(connectionString)
                .AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // TODO: should use proper authentication
            // app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                if (authHeader == "NmQ4NDYxMzkyM2VhNDVkN2E0MTQ5OGZlNzI2ZGE2Nzc6NTBlNWU1MmY2YmMxNDllMzhjMWY2MTQ5MDliZWZjNTg=")
                {
                    // Call the next delegate/middleware in the pipeline
                    await next();
                }
                else
                {
                    throw new AuthenticationException("not authorized to access treecat");
                }

            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<CacheService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
