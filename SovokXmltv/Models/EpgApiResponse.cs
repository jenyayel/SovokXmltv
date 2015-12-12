﻿

using Newtonsoft.Json;

namespace SovokXmltv.Models
{
    public class EpgApiResponse : BaseApiResponse
    {
        [JsonProperty(PropertyName = "epg")]
        public ApiProgram[] Programs { get; set; }
    }

    public class ApiProgram
    {
        [JsonProperty(PropertyName = "progname")]
        public string ProgramName { get; set; }

        public string Description { get; set; }


        [JsonProperty(PropertyName = "ut_start")]
        public double ProgramStartDateTime { get; set; }


        [JsonProperty(PropertyName = "ut_end")]
        public double ProgramEndDateTime { get; set; }


        [JsonProperty(PropertyName = "t_start")]
        public string ProgramStartTime{ get; set; }
    }
}
