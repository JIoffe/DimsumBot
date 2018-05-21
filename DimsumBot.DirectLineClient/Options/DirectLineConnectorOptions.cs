using System;

namespace DimsumBot.DirectLineClient.Options
{
    [Serializable]
    public class DirectLineConnectorOptions
    {
        public string BotSecret { get; set; }
        public string BotId { get; set; }
    }
}
