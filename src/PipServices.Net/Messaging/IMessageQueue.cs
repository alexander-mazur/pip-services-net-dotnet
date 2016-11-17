using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Run;

namespace PipServices.Net.Messaging
{
    public interface IMessageQueue : IConfigurable, IOpenable, IClosable, ICleanable
    {
        MessagingCapabilities Capabilities { get; }
        long MessageCount { get; }

        void Send(string correlationId, MessageEnvelop envelop);
        void SendAsObject(string correlationId, string messageType, object message);
        MessageEnvelop Peek(string correlationId);
        IEnumerable<MessageEnvelop> PeekBatch(string correlationId, int messageCount);
        MessageEnvelop Receive(string correlationId, long timeout);

        void RenewLock(MessageEnvelop message, long lockTimeout);
        void Complete(MessageEnvelop message);
        void Abandon(MessageEnvelop message);
        void MoveToDeadLetter(MessageEnvelop message);

        void Listen(string correlationId, IMessageReceiver receiver);
        void BeginListen(string correlationId, IMessageReceiver receiver);
        void EndListen(string correlationId);
    }
}