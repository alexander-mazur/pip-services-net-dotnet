using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;

namespace PipServices.Net.Messaging
{
    public sealed class MemoryMessageQueue : IMessageQueue, IReferenceable
    {
        private readonly long _defaultLockTimeout = 30000;
        private readonly long _defaultWaitTimeout = 5000;

        private string _name;
        private readonly object _lock = new object();
        private Queue<MessageEnvelop> _messages = new Queue<MessageEnvelop>();
        private int _lockTokenSequence;
        private readonly IDictionary<int, LockedMessage> _lockedMessages = new Dictionary<int, LockedMessage>();
        private readonly CompositeLogger _logger = new CompositeLogger();
        private readonly CompositeCounters _counters = new CompositeCounters();
        private MessagingCapabilities _capabilities = new MessagingCapabilities(
                true, true, true, true, true, true, true, false, true);
        private ManualResetEvent _receiveEvent = new ManualResetEvent(false);
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        private class LockedMessage
        {
            public MessageEnvelop Message { get; set; }
            public DateTime ExpirationTimeUtc { get; set; }
            public TimeSpan Timeout { get; set; }
        }

        public MemoryMessageQueue(string name = null)
        {
            _name = name;
        }

        public void Configure(ConfigParams config)
        {
            // If name is not defined get is from name property
            if (_name == null)
                _name = config.GetAsNullableString("name");

            // Or get name from descriptor
            if (_name == null)
            {
                var descriptorStr = config.GetAsNullableString("descriptor");
                var descriptor = Descriptor.FromString(descriptorStr);
                _name = descriptor.Name;
            }
        }

        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
        }

        public string Name
        {
            get { return _name ?? "undefined"; }
        }

        public MessagingCapabilities Capabilities { get; }


        public Task OpenAsync(string correlationId)
        {
            _logger.Trace(correlationId, "Opened queue {0}", this);

            return Task.Delay(0);
        }

        public async Task CloseAsync(string correlationId)
        {
            _cancel.Cancel();
            _receiveEvent.Set();

            _logger.Trace(null, "Closed queue {0}", this);

            await Task.Delay(0);
        }

        public long MessageCount
        {
            get
            {
                lock (_lock)
                {
                    return _messages.Count;
                }
            }
        }

        public async Task SendAsync(string correlationId, MessageEnvelop message)
        {
            if (message == null) return;

            await Task.Yield();
            //await Task.Delay(0);

            lock (_lock)
            {
                message.SentTimeUtc = DateTime.UtcNow;

                // Add message to the queue
                _messages.Enqueue(message);
            }

            // Release threads waiting for messages
            _receiveEvent.Set();

            _counters.IncrementOne("queue." + Name + ".sent_messages");
            _logger.Debug(correlationId, "Sent message {0} via {1}", message, this);
        }

        public async Task SendAsObjectAsync(string correlationId, string messageType, object message)
        {
            var envelop = new MessageEnvelop(correlationId, messageType, message);
            await SendAsync(correlationId, envelop);
        }

        public async Task<MessageEnvelop> PeekAsync(string correlationId)
        {
            MessageEnvelop message = null;

            lock(_lock) {
                // Pick a message
                if (_messages.Count > 0)
                    message = _messages.Peek();
            }

            if (message != null)
                _logger.Trace(correlationId, "Peeked message {0} on {1}", message, this);

            return await Task.FromResult(message);
        }

        public async Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount)
        {
            List<MessageEnvelop> messages = null;

            lock (_lock)
            {
                messages = _messages.ToArray().Take(messageCount).ToList();
            }

            _logger.Trace(null, "Peeked {0} messages on {1}", messages.Count, this);

            return await Task.FromResult(messages);
        }


        public async Task<MessageEnvelop> ReceiveAsync(string correlationId, long waitTimeout)
        {
            await Task.Delay(0);

            lock (_lock)
            {
                if (_messages.Count == 0)
                    _receiveEvent.Reset();
                else
                    _receiveEvent.Set();
            }

            _receiveEvent.WaitOne(TimeSpan.FromMilliseconds(waitTimeout));

            MessageEnvelop message = null;

            lock (_lock)
            {
                if (_messages.Count == 0)
                    return null;

                // Get message the the queue
                message = _messages.Dequeue();

                if (message != null)
                {
                    // Generate and set locked token
                    var lockedToken = _lockTokenSequence++;
                    message.Reference = lockedToken;

                    // Add messages to locked messages list
                    var lockedMessage = new LockedMessage
                    {
                        ExpirationTimeUtc = DateTime.UtcNow.AddMilliseconds(waitTimeout),
                        Message = message,
                        Timeout = TimeSpan.FromMilliseconds(waitTimeout)
                    };
                    _lockedMessages.Add(lockedToken, lockedMessage);
                }
            }

            if (message != null)
            {
                _counters.IncrementOne("queue." + _name + ".received_messages");
                _logger.Debug(message.CorrelationId, "Received message {0} via {1}", message, this);
            }

            return message;
        }

        public async Task RenewLockAsync(MessageEnvelop message, long lockTimeout)
        {
            if (message.Reference == null) return;

            lock (_lock)
            {
                // Get message from locked queue
                LockedMessage lockedMessage = null;
                int lockedToken = (int)message.Reference;

                // If lock is found, extend the lock
                if (_lockedMessages.TryGetValue(lockedToken, out lockedMessage))
                {
                    // Todo: Shall we skip if the message already expired?
                    if (lockedMessage.ExpirationTimeUtc > DateTime.UtcNow)
                    {
                        lockedMessage.ExpirationTimeUtc = DateTime.UtcNow.Add(lockedMessage.Timeout);
                    }
                }
            }

            _logger.Trace(message.CorrelationId, "Renewed lock for message {0} at {1}", message, this);

            await Task.Delay(0);
        }

        public async Task AbandonAsync(MessageEnvelop message)
        {
            if (message.Reference == null) return;

            lock (_lock)
            {
                // Get message from locked queue
                int lockedToken = (int)message.Reference;
                LockedMessage lockedMessage = null;
                if (_lockedMessages.TryGetValue(lockedToken, out lockedMessage))
                {
                    // Remove from locked messages
                    _lockedMessages.Remove(lockedToken);
                    message.Reference = null;

                    // Skip if it is already expired
                    if (lockedMessage.ExpirationTimeUtc <= DateTime.UtcNow)
                        return;
                }
                // Skip if it absent
                else return;
            }

            _logger.Trace(message.CorrelationId, "Abandoned message {0} at {1}", message, this);

            // Add back to the queue
            await SendAsync(message.CorrelationId, message);
        }

        public async Task CompleteAsync(MessageEnvelop message)
        {
            if (message.Reference == null) return;

            lock (_lock)
            {
                int lockKey = (int)message.Reference;
                _lockedMessages.Remove(lockKey);
                message.Reference = null;
            }

            _logger.Trace(message.CorrelationId, "Completed message {0} at {1}", message, this);

            await Task.Delay(0);
        }

        public async Task MoveToDeadLetterAsync(MessageEnvelop message)
        {
            if (message.Reference == null) return;

            lock (_lock)
            {
                int lockKey = (int)message.Reference;
                _lockedMessages.Remove(lockKey);
                message.Reference = null;
            }

            _counters.IncrementOne("Queue." + _name + ".DeadMessages");
            _logger.Trace(message.CorrelationId, "Moved to dead message {0} at {1}", message, this);

            await Task.Delay(0);
        }

        public async Task ListenAsync(string correlationId, IMessageReceiver receiver)
        {
            _logger.Trace(null, "Started listening messages at {0}", this);

            // Create new token source
            _cancel = new CancellationTokenSource();

            while (!_cancel.Token.IsCancellationRequested)
            {
                var message = await ReceiveAsync(correlationId, 1000);

                if (message != null)
                {
                    try
                    {
                        if (!_cancel.IsCancellationRequested)
                            await receiver.ReceiveMessageAsync(message, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(null, ex, "Failed to process the message");
                        //await AbandonAsync(message);
                    }
                }
            }
        }

        public void BeginListen(string correlationId, IMessageReceiver receiver)
        {
            ThreadPool.QueueUserWorkItem(async delegate {
                await ListenAsync(correlationId, receiver);
            });
        }

        public void EndListen(string correlationId)
        {
            _cancel.Cancel();
        }

        public Task ClearAsync(string correlationId)
        {
            lock (_lock)
            {
                // Clear messages
                _messages.Clear();
                _lockedMessages.Clear();
            }

            _logger.Trace(correlationId, "Cleared queue {0}", this);

            return Task.Delay(0);
        }

        public override string ToString()
        {
            return "[" + Name + "]";
        }
    }
}
