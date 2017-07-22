using Newtonsoft.Json;

namespace SovokXmltv.Sovok
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
