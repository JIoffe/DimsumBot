using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DimsumBot.Extensions;
using DimsumBot.Model.Shared.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace DimsumBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            var text = activity?.Value as string ?? activity.Text;

            //English mother@$!, do you speak it!?
            var isSupportedLanguage = Regex.IsMatch(text, @"^[a-zA-Z0-9\s\32-\151]+$");

            if (!isSupportedLanguage)
            {
                var responses = new String[]
                {
                    "我不会说中文！Please only use English words. I am but a simple chat bot.",
                    "Whoa, trailblazer. Testing the limits of cognitive AI! Nice! Please only use English.",
                    "I'm sorry, I only speak English! Let's talk about dim sum.",
                    "我不懂! Try asking again in English please. Ask me about dim sum!"
                };

                await context.DispatchAsync(responses.RandomElementOrDefault());
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                await context.Forward(new LuisQueryDialog(), ResumeAfter_LuisQuery, activity, CancellationToken.None);
            }
        }

        private async Task ResumeAfter_LuisQuery(IDialogContext context, IAwaitable<object> result)
        {
            //To simplify, we either throw the activity back to the parent or not
            var rejectedActivity = await result as IMessageActivity;

            if(rejectedActivity != null)
            {
                await context.Forward(new QnADialog(), ResumeAfter_QnADialog, rejectedActivity, CancellationToken.None);
                return;
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfter_QnADialog(IDialogContext context, IAwaitable<bool> result)
        {
            var success = await result;
            if(!success)
            {
                var failureMessages = new String[]
                {
                    "I'm sorry, I have no idea what you're talking about.",
                    "Alright, you stumped me. Please try something else!",
                    "What are you talking about?",
                    "Oh, I just don't understand!",
                    "我不懂!"
                };

                await context.DispatchAsync(failureMessages.RandomElementOrDefault());
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}