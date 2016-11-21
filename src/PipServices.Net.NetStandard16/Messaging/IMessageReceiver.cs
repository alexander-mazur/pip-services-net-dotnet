using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public interface IMessageReceiver
    {
        Task ReceiveMessageAsync(MessageEnvelop message, IMessageQueue queue);
    }
}