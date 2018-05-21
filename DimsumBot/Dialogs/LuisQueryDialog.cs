using CommonServiceLocator;
using DimsumBot.Extensions;
using DimsumBot.Model.Shared.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;

namespace DimsumBot.Dialogs
{
    /// <summary>
    /// Highly-Simplified for dim sum bot
    /// </summary>
    [Serializable]
    public class LuisQueryDialog: LuisDialog<object>
    {
        public LuisQueryDialog()
            : base(GetLuisService())
        {

        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            var greetings = new string[]
            {
                "Hello! Let's talk dim sum.",
                "Hey! I'm here to help you find out more about delicious dim sum",
                "你好! Ni Hao! That's all the Chinese I know, though. Please talk to me in English! Ask me about dim sum.",
                "Yo! It's dim sum time."
            };

            await context.DispatchAsync(greetings.RandomElementOrDefault());

            context.Done(string.Empty);
        }

        [LuisIntent("AskRecommendation")]
        public async Task AskRecommendationIntent(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            await context.DispatchAsync("Cha Shao Su and Liu Lian Su are a killer combo of savory and sweet. Try them! Ask me more about them.");
            context.Done(string.Empty);
        }

        [LuisIntent("AskLocation")]
        public async Task AskLocationIntent(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            await context.DispatchAsync("Jing Fong in Chinatown! It's the best one!");
            context.Done(string.Empty);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, IAwaitable<IMessageActivity> item, LuisResult result)
        {
            var activity = await item;
            context.Done(activity);
        }

        private static ILuisService GetLuisService() => ServiceLocator.Current.GetInstance<ILuisService>();
    }
}