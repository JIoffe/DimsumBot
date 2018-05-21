using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DimsumBot.Extensions;
using DimsumBot.Model.Shared.Wechat;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace DimsumBot.Dispatch
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly string _wechatOutgoingURI;

        public MessageDispatcher(string wechatOutgoingUri)
        {
            _wechatOutgoingURI = wechatOutgoingUri;
        }

        public async Task DispatchAsync(IDialogContext context, IMessageActivity activity)
        {
            if (context.IsWechat())
            {
                await DispatchToWechatAsync(context, activity);
            }

            await context.PostAsync(activity);
        }

        private async Task DispatchToWechatAsync(IDialogContext context, IMessageActivity activity)
        {
            using (var client = new HttpClient())
            {
                var wechatMessage = new WechatMessage
                {
                    MessageType = WechatMessageTypes.TEXT,
                    Content = activity.Text,
                    ToUserName = context.GetChannelUserId()
                };

                var content = new StringContent(JsonConvert.SerializeObject(wechatMessage), Encoding.UTF8);
                await client.PostAsync(_wechatOutgoingURI, content);
            }
        }
    }
}