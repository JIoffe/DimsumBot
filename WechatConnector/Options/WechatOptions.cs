using System;

namespace WechatConnector.Options
{
    [Serializable]
    public class WechatOptions
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string TokenURI { get; set; }
        public string CustomerEndpoint { get; set; }
        public string MediaUploadEndpoint { get; set; }
    }
}
