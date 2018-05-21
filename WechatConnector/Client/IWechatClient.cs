using DimsumBot.Model.Shared.Wechat;
using System.Threading.Tasks;

namespace WechatConnector.Client
{
    public interface IWechatClient
    {
        Task PostMessage(WechatMessage msg);
        Task<string> UploadMedia(string type, string filePath);
    }
}
