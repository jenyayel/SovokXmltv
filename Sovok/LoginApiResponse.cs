﻿using Newtonsoft.Json;

namespace SovokXmltv.Sovok
{
    public class LoginApiResponse : BaseApiResponse
    {
        [JsonProperty(PropertyName = "sid_name")]
        public string SessionCookieName { get; set; }

        [JsonProperty(PropertyName = "sid")]
        public string SessionCookieValue { get; set; }
    }
}
