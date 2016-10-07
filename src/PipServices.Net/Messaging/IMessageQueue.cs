using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Run;

namespace PipServices.Net.Messaging
{
    public interface IMessageQueue : IConfigurable, IOpenable, IClosable
    {
        MessagingCapabilities Capabilities { get; }
        long MessageCount { get; }

        void Send(MessageEnvelop envelop);
        void Send(string correlationId, string messageType, object message);
        MessageEnvelop Peek();
        IEnumerable<MessageEnvelop> PeekBatch(int messageCount);
        MessageEnvelop Receive(long timeout);

        void RenewLock(MessageEnvelop message);
        void Complete(MessageEnvelop message);
        void Abandon(MessageEnvelop message);
        void MoveToDeadLetter(MessageEnvelop message);

        void OnMessage(IMessageReceiver receiver);
        void BeginOnMessage(IMessageReceiver receiver);
        void EndOnMessage();

        void Clear();
    }
}