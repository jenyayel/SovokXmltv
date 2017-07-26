using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SovokXmltv.Sovok;
using System;
using System.Text;

namespace SovokXmltv
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SovokClient>();
        }

        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                if (context.Request.Path.Value != "/")
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var user = context.Request.Query["user"];
                var password = context.Request.Query["password"];
                var period = context.Request.Query["period"];

                (SettingsApiResponse settings, ChannelsListApiResponse channels, Epg3ApiResponse epg) apiResult;

                try
                {
                    apiResult = await serviceProvider.GetService<SovokClient>().GetAggregated(user, password, period);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 400;
                    var buffer = Encoding.UTF8.GetBytes(ex.Message);
                    await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                    return;
                }

                context.Response.Headers.Add("content-type", "application/xml");
                using (var writer = new XmlTvWriter(context.Response.Body))
                {
                    await writer.Write(apiResult.settings, apiResult.channels, apiResult.epg);
                }
            });
        }
    }
}
