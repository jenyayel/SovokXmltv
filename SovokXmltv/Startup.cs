using System;
using SovokXmltv.Helpers;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;
using SovokXmltv.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;
using System.Xml;

namespace SovokXmltv
{
    public class Startup
    {
        private const string API_ENDPOINT = "http://api.sovok.tv/v2.2/json";
        private const string ICONS_PREFIX_HOST = "http://sovok.tv";
        private readonly StringValues EPG_DEFAULT_PERIOD = "12";

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
                var period = context.Request.Query.ContainsKey("period") ? context.Request.Query["period"] : EPG_DEFAULT_PERIOD;

                if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(password))
                {
                    await context.SetError(400, "Missing credentials");
                    return;
                }


                using (HttpClient client = new HttpClient())
                {
                    // get session token
                    var loginResult = await client.GetApiResultAsync<LoginApiResponse>($"{API_ENDPOINT}/login?login={user}&pass={password}");
                    if (loginResult == null || loginResult.Error != null)
                    {
                        await context.SetError(400, loginResult?.Error.Message);
                        return;
                    }

                    // add authorization
                    client.DefaultRequestHeaders.Add("Cookie", $"{loginResult.SessionCookieName}={loginResult.SessionCookieValue}");

                    // prepare tasks for API calls
                    var settingsResultTask = client.GetApiResultAsync<SettingsApiResponse>($"{API_ENDPOINT}/settings");
                    var channelResultTask = client.GetApiResultAsync<ChannelsListApiResponse>($"{API_ENDPOINT}/channel_list2");
                    var epgResultTask = client.GetApiResultAsync<Epg3ApiResponse>($"{API_ENDPOINT}/epg3?dtime={DateTime.UtcNow.ToUnixTime()}&period={period}");

                    // execute calls
                    await Task.WhenAll(new Task[] { settingsResultTask, channelResultTask, epgResultTask });
                    var settingsResult = settingsResultTask.Result;
                    var channelResult = channelResultTask.Result;
                    var epgResult = epgResultTask.Result;

                    // validate results
                    if (settingsResult == null || settingsResult.Error != null)
                    {
                        await context.SetError(400, settingsResult?.Error.Message);
                        return;
                    }

                    if (channelResult == null || channelResult.Error != null)
                    {
                        await context.SetError(400, channelResult?.Error.Message);
                        return;
                    }

                    if (epgResult == null || epgResult.Error != null)
                    {
                        await context.SetError(400, epgResult?.Error.Message);
                        return;
                    }

                    Trace.TraceError($"Total channels are [{channelResult.Channels.Count()}]; total channels in EPG [{epgResult.Channels.Count()}].");

                    // write output
                    context.Response.Headers.Add("content-type", "application/xml");

                    using (XmlWriter writer = XmlWriter.Create(context.Response.Body, new XmlWriterSettings { Async = true, Indent = true, }))
                    {
                        writer.WriteDocType("tv", "xmltv.dtd", "SYSTEM", null);
                        writer.WriteStartElement("tv");
                        writer.WriteAttribute("generator-info-name", "SovokXmltv");

                        foreach (var channel in channelResult.Channels)
                        {
                            writer.WriteStartElement("channel");
                            writer.WriteAttribute("id", channel.Id);

                            writer.WriteElementString("display-name", channel.Name);
                            writer.WriteStartElement("icon");
                            writer.WriteAttribute("src", ICONS_PREFIX_HOST + channel.Icon);
                            writer.WriteEndElement(); // icon

                            writer.WriteEndElement(); //channel
                        }

                        await writer.FlushAsync();

                        foreach (var channel in epgResult.Channels)
                        {
                            foreach (var programm in channel.Programs)
                            {
                                writer.WriteStartElement("programme");
                                writer.WriteAttribute("channel", channel.Id);
                                if (programm.ProgramStartDateTime != 0)
                                    writer.WriteAttribute("start", programm.ProgramStartDateTime.ToDateTime().ToString("yyyyMMddHHmmss zz00"));
                                if (programm.ProgramEndDateTime != 0)
                                    writer.WriteAttribute("stop", programm.ProgramEndDateTime.ToDateTime().ToString("yyyyMMddHHmmss zz00"));

                                writer.WriteElementString("title", programm.ProgramName);
                                if (!String.IsNullOrEmpty(programm.Description))
                                    writer.WriteElementString("desc", programm.Description);

                                writer.WriteEndElement(); //programme
                            }
                            await writer.FlushAsync();
                        }

                        writer.WriteEndElement(); //tv
                        await writer.FlushAsync();
                    }
                }
                watch.Stop();
                Trace.TraceInformation($"All done in {watch.Elapsed}");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
