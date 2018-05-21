using System;

namespace DimsumBot.Core.Devices.Options
{
    [Serializable]
    public class DeviceRegistrationOptions
    {
        public string CollectionName { get; set; }
        public string DbName { get; set; }
    }
}
