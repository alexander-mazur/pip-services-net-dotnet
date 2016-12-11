using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public interface IMessageReceiver
    {
        Task ReceiveMessageAsync(MessageEnvelop envelop, IMessageQueue queue);
    }
}