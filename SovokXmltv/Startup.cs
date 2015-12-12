using System;
using SovokXmltv.Helpers;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;
using SovokXmltv.Models;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SovokXmltv
{
    public class Startup
    {
        private const string API_ENDPOINT = "http://api.sovok.tv/v2.2/json";

        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseIISPlatformHandler();
            if (String.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                app.UseDeveloperExceptionPage();
                app.UseRuntimeInfoPage(); // default path is /runtimeinfo
            }

            app.Run(async (context) =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                var user = context.Request.Query["user"];
                var password = context.Request.Query["password"];

                if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(password))
                {
                    await context.SetError(400, "Missing credentials");
                    return;
                }


                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(60);
                    var loginResult = await client.GetApiResultAsync<LoginApiResponse>($"{API_ENDPOINT}/login?login={user}&pass={password}");
                    if (loginResult == null || loginResult.Error != null)
                    {
                        await context.SetError(400, loginResult?.Error.Message);
                        return;
                    }

                    client.DefaultRequestHeaders.Add("Cookie", $"{loginResult.SessionCookieName}={loginResult.SessionCookieValue}");
                    var channelResult = await client.GetApiResultAsync<ChannelsListApiResponse>($"{API_ENDPOINT}/channel_list2");
                    if (channelResult == null || channelResult.Error != null)
                    {
                        await context.SetError(400, channelResult?.Error.Message);
                        return;
                    }

                    var today = DateTime.UtcNow.ToString("ddMMyy"); // TODO: timezone missing
                    var tasks = channelResult
                        .Channels
                        .Select(c => client.GetApiResultAsync<EpgApiResponse>($"{API_ENDPOINT}/epg?cid={c.Id}&day={today}", c, true))
                        .ToArray();

                    EpgApiResponse[] fullEpg = null;
                    try
                    {
                        fullEpg = await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Some tasks failed to finish due to {e}");
                    }

                    if (fullEpg == null)
                    {
                        await context.SetError(400, "Failed to get EPG");
                        return;
                    }

                    var epgErrors = fullEpg.Where(c => c != null && c.Error != null);

                    foreach (var c in epgErrors)
                        Trace.TraceError($"API returned error [{c.Error.Message}] for channel {(c.Context as ApiChannel)?.Id}.");

                    var epgSuccess = fullEpg.Where(c => c != null && c.Error == null);
                    
                    // TODO: build XML

                    await context.Response.WriteAsync($"Got EPG for {epgSuccess.Count()}");
                }
                watch.Stop();
                Trace.TraceInformation($"All done in {watch.Elapsed}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
