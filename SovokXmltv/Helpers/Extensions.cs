using Microsoft.AspNet.Http;
using Newtonsoft.Json;
using Polly;
using SovokXmltv.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace SovokXmltv.Helpers
{
    public static class Extensions
    {
        private static MemoryCache _cache = new MemoryCache(new MemoryCacheOptions { CompactOnMemoryPressure = false });
        private static Policy _requestPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3,
            (retryCount) =>
            {
                return TimeSpan.FromMilliseconds(50 + (50 * retryCount));
            },
            (ex, retryCount) =>
            {
                Trace.TraceInformation($"Retrying {retryCount} time due to {ex}");
            });


        public static async Task<T> GetApiResultAsync<T>(this HttpClient self, string requestUri, object related = null, bool cache = false) where T : BaseApiResponse
        {
            string plainResult = null;
            T result = null;

            if (cache && _cache.TryGetValue<T>(requestUri, out result))
            {
                Trace.TraceInformation($"Retrieved from cache [{requestUri}]...");
                return result;
            }

            try
            {
                plainResult = await _requestPolicy.ExecuteAsync(() => self.GetStringAsync(requestUri));
                Trace.TraceInformation($"Finished [{requestUri}]...");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to get API response from [{requestUri}] due to {ex}");
                return null;
            }

            result = JsonConvert.DeserializeObject<T>(plainResult);
            if (related != null)
                result.Context = related;

            if (cache && result.Error == null)
                _cache.Set(requestUri, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });

            return result;
        }

        public static async Task SetError(this HttpContext self, int statusCode, string message = null)
        {
            self.Response.StatusCode = statusCode;
            if (!String.IsNullOrEmpty(message))
                await self.Response.WriteAsync(message);
            else
                await Task.FromResult(0);
        }
    }
}
