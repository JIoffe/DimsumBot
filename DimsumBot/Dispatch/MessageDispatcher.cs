using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DimsumBot.Extensions;
using DimsumBot.Model.Shared.Wechat;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            //The connector could handle this conversion instead...
            var wechatMessage = ConvertMessage(context, activity);

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(wechatMessage), Encoding.UTF8);
                await client.PostAsync(_wechatOutgoingURI, content);
            }
        }

        private WechatMessage ConvertMessage(IDialogContext context, IMessageActivity activity)
        {
            //The connector could handle this conversion instead...
            var wechatMessage = new WechatMessage
            {
                ToUserName = context.GetChannelUserId()
            };

            var richCards = activity.Attachments?.Where(att => att.ContentType.Equals("application/vnd.microsoft.card.hero", System.StringComparison.InvariantCultureIgnoreCase));
            if (richCards?.Count() > 0)
            {
                wechatMessage.MessageType = WechatMessageTypes.RICH_MEDIA;
                wechatMessage.Articles = richCards.Select(att =>
                {
                    var richCard = att.Content as HeroCard;
                    return new WechatArticle
                    {
                        Title = richCard.Title,
                        Description = richCard.Subtitle ?? richCard.Text,
                        PicUrl = richCard.Images?.FirstOrDefault()?.Url ?? string.Empty
                    };
                });

                return wechatMessage;
            }

            var image = activity.Attachments?.FirstOrDefault(att => att.ContentType.Contains("image"));
            if (image != null)
            {
                wechatMessage.MessageType = WechatMessageTypes.IMAGE;
                wechatMessage.MediaId = image.ContentUrl;
                return wechatMessage;
            }

            wechatMessage.MessageType = WechatMessageTypes.TEXT;
            wechatMessage.Content = activity.Text;
            return wechatMessage;
        }
    }
}