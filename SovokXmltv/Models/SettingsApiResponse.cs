using Newtonsoft.Json;

namespace SovokXmltv.Models
{
    public class SettingsApiResponse : BaseApiResponse
    {
        [JsonProperty(PropertyName = "timezone")]
        public string Timezone { get; set; }
    }
}
