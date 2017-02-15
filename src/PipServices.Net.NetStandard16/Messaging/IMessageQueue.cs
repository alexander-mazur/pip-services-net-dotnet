using PipServices.Commons.Run;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public interface IMessageQueue : IOpenable
    {
        string Name { get; }
        MessagingCapabilities Capabilities { get; }
        long? MessageCount { get; }

        Task SendAsync(string correlationId, MessageEnvelop envelop);
        Task SendAsync(string correlationId, string messageType, string message);
        Task SendAsObjectAsync(string correlationId, string messageType, object message);
        Task<MessageEnvelop> PeekAsync(string correlationId);
        Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount);
        Task<MessageEnvelop> ReceiveAsync(string correlationId, long waitTimeout);

        Task RenewLockAsync(MessageEnvelop message, long lockTimeout);
        Task CompleteAsync(MessageEnvelop message);
        Task AbandonAsync(MessageEnvelop message);
        Task MoveToDeadLetterAsync(MessageEnvelop message);

        Task ListenAsync(string correlationId, IMessageReceiver receiver);
        Task ListenAsync(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback);
        void BeginListen(string correlationId, IMessageReceiver receiver);
        void BeginListen(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback);
        void EndListen(string correlationId);
    }
}