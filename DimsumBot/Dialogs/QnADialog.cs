using CommonServiceLocator;
using DimsumBot.Extensions;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DimsumBot.Dialogs
{
    [Serializable]
    public class QnADialog: IDialog<bool>
    {
        private static double CONFIDENCE_THRESHOLD = 0.3D;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            var q = activity.Value as string ?? activity.Text;

            var qnaService = GetQnAService();
            var qnaResults = await Microsoft.Bot.Builder.CognitiveServices.QnAMaker.Extensions.QueryServiceAsync(GetQnAService(), q);

            var bestAnswer = qnaResults.Answers.FirstOrDefault(a => a.Score >= CONFIDENCE_THRESHOLD);
            if (bestAnswer != null)
            {
                await context.DispatchAsync(bestAnswer.Answer);
                context.Done(true);
            }
            else
            {
                context.Done(false);
            }
        }

        private static IQnAService GetQnAService() => ServiceLocator.Current.GetInstance<IQnAService>();
    }
}