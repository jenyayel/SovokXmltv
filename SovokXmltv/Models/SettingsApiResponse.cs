using Newtonsoft.Json;

namespace SovokXmltv.Models
{
    public class SettingsApiResponse : BaseApiResponse
    {
        public ApiSettings Settings { get; set; }
    }

    public class ApiSettings
    {
        public string Timezone { get; set; }
    }
}
