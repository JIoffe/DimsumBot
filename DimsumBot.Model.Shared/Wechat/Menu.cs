using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DimsumBot.Model.Shared.Wechat
{
    [Serializable]
    public class Menu
    {
        [JsonProperty("button")]
        public IList<MenuItem> MenuItems { get; set; }
    }
}
