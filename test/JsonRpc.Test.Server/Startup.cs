using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using JsonRpc.Host;
using Microsoft.Extensions.Logging;

namespace JsonRpc.Test.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<Services>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Information);
            app.UseJsonRpc("/api");
            JsonRpcProcessor.Instance.Configure(new JsonRpcHostOptions()
            {
               AssembliesToScan = new[] { typeof(Services).GetTypeInfo().Assembly }
            });
        }
    }
}
