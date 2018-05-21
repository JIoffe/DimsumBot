using Microsoft.Bot.Builder.Dialogs;
using System;
using DimsumBot.Model.Shared;
using System.Threading.Tasks;
using CommonServiceLocator;
using DimsumBot.Dispatch;
using Microsoft.Bot.Connector;

namespace DimsumBot.Extensions
{
    public static class DialogContextExtensions
    {
        public static bool IsWechat(this IDialogContext context)
        {
            var channelData = context.GetChannelData();
            return "wechat".Equals(channelData?.Subchannel, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string GetChannelUserId(this IDialogContext context)
            => context.GetChannelData()?.UserId;

        public static ChannelData GetChannelData(this IDialogContext context)
        {
            ChannelData channelData = null;
            context.Activity.TryGetChannelData(out channelData);
            return channelData;
        }

        public static async Task DispatchAsync(this IDialogContext context, string message)
        {
            var activity = context.MakeMessage();
            activity.Type = ActivityTypes.Message;
            activity.Text = message;

            await ServiceLocator.Current.GetInstance<IMessageDispatcher>().DispatchAsync(context, activity);
        }

        public static async Task DispatchAsync(this IDialogContext context, IMessageActivity activity)
            => await ServiceLocator.Current.GetInstance<IMessageDispatcher>().DispatchAsync(context, activity);
    }
}