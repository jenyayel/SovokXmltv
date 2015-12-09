using System;
using SovokXmltv.Helpers;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using SovokXmltv.Models;
using System.Threading.Tasks;

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
                    var loginResult = await client.GetStringAsync<LoginApiResponse>($"{API_ENDPOINT}/login?login={user}&pass={password}");
                    if (loginResult.Error != null)
                    {
                        await context.SetError(400, loginResult.Error.Message);
                        return;
                    }

                    client.DefaultRequestHeaders.Add("Cookie", $"{loginResult.SessionCookieName}={loginResult.SessionCookieValue}");
                    var channelResult = await client.GetStringAsync<ChannelsListApiResponse>($"{API_ENDPOINT}/channel_list2");
                    if (channelResult.Error != null)
                    {
                        await context.SetError(400, channelResult.Error.Message);
                        return;
                    }

                    var today = DateTime.Now.ToString("ddMMyy"); // TODO: timezone missing
                    var tasks = channelResult
                        .Channels
                        .Select(c => client.GetStringAsync<EpgApiResponse>($"{API_ENDPOINT}/epg?cid={c.Id}&day={today}"))
                        .ToArray();

                    var fullEpg = await Task.WhenAll(tasks);


                    await context.Response.WriteAsync(JsonConvert.SerializeObject(fullEpg), System.Text.Encoding.UTF8);
                }

            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
