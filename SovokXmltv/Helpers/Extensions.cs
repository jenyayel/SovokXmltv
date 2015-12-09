using Microsoft.AspNet.Http;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SovokXmltv.Helpers
{
    public static class Extensions
    {
        public static async Task<T> GetStringAsync<T>(this HttpClient self, string requestUri)
        {
            var plainResult = await self.GetStringAsync(requestUri);
            return JsonConvert.DeserializeObject<T>(plainResult);
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
