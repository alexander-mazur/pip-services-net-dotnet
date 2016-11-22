using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Run;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public interface IMessageQueue : IConfigurable, IOpenable, IClosable, ICleanable
    {
        string Name { get; }
        MessagingCapabilities Capabilities { get; }
        long MessageCount { get; }

        Task SendAsync(string correlationId, MessageEnvelop envelop);
        Task SendAsObjectAsync(string correlationId, string messageType, object message);
        Task<MessageEnvelop> PeekAsync(string correlationId);
        Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount);
        Task<MessageEnvelop> ReceiveAsync(string correlationId, long timeout);

        Task RenewLockAsync(MessageEnvelop message, long lockTimeout);
        Task CompleteAsync(MessageEnvelop message);
        Task AbandonAsync(MessageEnvelop message);
        Task MoveToDeadLetterAsync(MessageEnvelop message);

        Task ListenAsync(string correlationId, IMessageReceiver receiver);
        void BeginListen(string correlationId, IMessageReceiver receiver);
        void EndListen(string correlationId);
    }
}