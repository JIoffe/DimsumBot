using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace DimsumBot.Dispatch
{
    /// <summary>
    /// Abstracts away message processing for logging and/or additional dispatching requests
    /// </summary>
    public interface IMessageDispatcher
    {
        Task DispatchAsync(IDialogContext context, IMessageActivity activity);
    }
}
