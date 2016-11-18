namespace PipServices.Net.Messaging
{
    public interface IMessageReceiver
    {
        void ReceiveMessage(MessageEnvelop message, IMessageQueue queue);
    }
}