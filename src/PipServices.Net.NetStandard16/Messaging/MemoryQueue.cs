using PipServices.Commons.Auth;
using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Count;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    /// <summary>
    /// Local message queue to be used in automated tests
    /// </summary>
    public abstract class MessageQueue : IMessageQueue, IDescriptable, IReferenceable, IConfigurable, IOpenable, IClosable
    {
        protected CompositeLogger _logger = new CompositeLogger();
        protected CompositeCounters _counters = new CompositeCounters();
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        protected CredentialResolver _credentialResolver = new CredentialResolver();
        protected object _lock = new object();

        public MessageQueue(string name = null, ConfigParams config = null)
        {
            Name = name;
            //Capabilities = new MessagingCapabilities(true, true, true, true, true, true, true, true, true);

            if (config != null) Configure(config);
        }

        public Descriptor GetDescriptor()
        {
            return new Descriptor("pip-services-net", "message-queue", Kind ?? "unknown", Name, "1.0");
        }

        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        public virtual void Configure(ConfigParams config)
        {
            Name = NameResolver.Resolve(config, Name);
            _connectionResolver.Configure(config, true);
            _credentialResolver.Configure(config, true);
        }

        public async virtual Task OpenAsync(string correlationId)
        {
            var connection = await _connectionResolver.ResolveAsync(correlationId);
            var credential = await _credentialResolver.LookupAsync(correlationId);
            await OpenAsync(correlationId, connection, credential);
        }

        public abstract Task OpenAsync(string correlationId, ConnectionParams connection, CredentialParams credential);

        public abstract Task CloseAsync(string correlationId);
        public abstract Task ClearAsync(string correlationId);

        public string Name { get; protected set; }
        protected string Kind { get; set; }

        public MessagingCapabilities Capabilities { get; protected set; }

        public abstract long? MessageCount { get; }

        public abstract Task SendAsync(string correlationId, MessageEnvelop message);

        public async Task SendAsync(string correlationId, string messageType, string message)
        {
            var envelop = new MessageEnvelop(correlationId, messageType, message);
            await SendAsync(correlationId, envelop);
        }

        public async Task SendAsObjectAsync(string correlationId, string messageType, object message)
        {
            var envelop = new MessageEnvelop(correlationId, messageType, message);
            await SendAsync(correlationId, envelop);
        }

        public abstract Task<MessageEnvelop> PeekAsync(string correlationId);
        public abstract Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount);
        public abstract Task<MessageEnvelop> ReceiveAsync(string correlationId, long waitTimeout);
        public abstract Task RenewLockAsync(MessageEnvelop message, long lockTimeout);
        public abstract Task AbandonAsync(MessageEnvelop message);
        public abstract Task CompleteAsync(MessageEnvelop message);
        public abstract Task MoveToDeadLetterAsync(MessageEnvelop message);

        public Task ListenAsync(string correlationId, IMessageReceiver receiver)
        {
            return ListenAsync(correlationId, receiver.ReceiveMessageAsync);
        }

        public abstract Task ListenAsync(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback);

        public void BeginListen(string correlationId, IMessageReceiver receiver)
        {
            BeginListen(correlationId, receiver.ReceiveMessageAsync);
        }

        public void BeginListen(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback)
        {
            ThreadPool.QueueUserWorkItem(async delegate {
                await ListenAsync(correlationId, callback);
            });
        }

        public abstract void EndListen(string correlationId);

        public override string ToString()
        {
            return "[" + Name + "]";
        }
    }
}
