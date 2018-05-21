using Newtonsoft.Json;
using System;

namespace DimsumBot.Model.Shared
{
    /// <summary>
    /// Additional data to attach on activities
    /// </summary>
    [Serializable]
    public class ChannelData
    {
        [JsonProperty("subchannel_id")]
        public string Subchannel { get; set; }
        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }
}
