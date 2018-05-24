using CommonServiceLocator;
using DimsumBot.Extensions;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
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
                var msg = context.MakeMessage();

                try
                {
                    var heroCard = JsonConvert.DeserializeObject<HeroCard>(bestAnswer.Answer);
                    msg.Attachments.Add(heroCard.ToAttachment());
                }catch(JsonSerializationException ex)
                {
                    //Couldn't deserialize answer...
                    msg.Text = bestAnswer.Answer;
                }

                //var heroCard = new HeroCard
                //{
                //    Title = "DIM SUM!",
                //    Subtitle = bestAnswer.Answer,
                //    Images = new CardImage[]
                //    {
                //        new CardImage("https://thumb1.shutterstock.com/display_pic_with_logo/3842924/390423109/stock-photo-shrimp-shumai-a-steamed-dish-to-enjoy-the-sweet-tenderness-of-dried-sakura-shrimp-390423109.jpg")
                //    }
                //};

                

                await context.DispatchAsync(msg);
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