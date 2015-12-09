using Newtonsoft.Json;

namespace SovokXmltv.Models
{
    public class ChannelsListApiResponse : BaseApiResponse
    {
        public ApiChannel[] Channels { get; set; }
    }


    public class ApiChannel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonProperty(PropertyName = "is_video")]
        public bool IsVideo { get; set; }

        public string Icon { get; set; }
        
        public int? Group { get; set; }
    }

}
