using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SovokXmltv.Sovok
{
    public class SovokClient
    {
        private const string API_ENDPOINT = "http://api.sovok.tv/v2.2/json";
        private const string EPG_DEFAULT_PERIOD = "28";
        private readonly TimeSpan EPG_START_FROM_NOW = TimeSpan.FromHours(-4);

        private readonly HttpClient _client;
        private readonly ILogger<SovokClient> _logger;


        public SovokClient(ILogger<SovokClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = new HttpClient();
        }

        public async Task<(SettingsApiResponse settings, ChannelsListApiResponse channels, Epg3ApiResponse epg)> GetAggregated(
            string user,
            string password,
            string period)
        {
            if (String.IsNullOrEmpty(user)) throw new ArgumentNullException(nameof(user));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (String.IsNullOrEmpty(period)) period = EPG_DEFAULT_PERIOD;

            // get session token
            var loginResult = await getApi<LoginApiResponse>($"{API_ENDPOINT}/login?login={user}&pass={password}");
            if (loginResult == null || loginResult.Error != null)
                throw new InvalidOperationException(loginResult?.Error.Message ?? "Failed to login");

            // add authorization
            _client.DefaultRequestHeaders.Add("Cookie", $"{loginResult.SessionCookieName}={loginResult.SessionCookieValue}");

            // prepare tasks for API calls
            var settingsResultTask = getApi<SettingsApiResponse>($"{API_ENDPOINT}/settings");
            var channelResultTask = getApi<ChannelsListApiResponse>($"{API_ENDPOINT}/channel_list2");
            var epgResultTask = getApi<Epg3ApiResponse>($"{API_ENDPOINT}/epg3?dtime={toUnixTime(DateTime.UtcNow.Add(EPG_START_FROM_NOW))}&period={period}");

            // execute calls
            await Task.WhenAll(new Task[] { settingsResultTask, channelResultTask, epgResultTask });
            var epgResult = epgResultTask.Result;

            // validate results
            if (settingsResultTask.Result == null || settingsResultTask.Result.Error != null)
                throw new InvalidOperationException(settingsResultTask.Result?.Error.Message ?? "Failed to get settings");

            if (channelResultTask.Result == null || channelResultTask.Result.Error != null)
                throw new InvalidOperationException(channelResultTask.Result?.Error.Message ?? "Failed to get channels");

            if (epgResultTask.Result == null || epgResultTask.Result.Error != null)
                throw new InvalidOperationException(epgResultTask.Result?.Error.Message ?? "Failed to get epg");

            return (settingsResultTask.Result, channelResultTask.Result, epgResultTask.Result);
        }

        private async Task<T> getApi<T>(string requestUri) where T : BaseApiResponse
        {

            using (var response = await _client.GetAsync(requestUri))
            {
                var payload = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"API returned {response.StatusCode}: {payload}");

                return JsonConvert.DeserializeObject<T>(payload);
            }
        }

        private static double toUnixTime(DateTime time) => (time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }
}
