using PipServices.Commons.Auth;
using PipServices.Commons.Connect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    /// <summary>
    /// Local message queue to be used in automated tests
    /// </summary>
    public class MemoryMessageQueue : MessageQueue
    {
        private ManualResetEvent _receiveEvent = new ManualResetEvent(false);
        private Queue<MessageEnvelop> _messages = new Queue<MessageEnvelop>();
        private int _lockTokenSequence = 0;
        private Dictionary<int, LockedMessage> _lockedMessages = new Dictionary<int, LockedMessage>();
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private bool _opened = false;

        private class LockedMessage
        {
            public MessageEnvelop Message { get; set; }
            public DateTime ExpirationTimeUtc { get; set; }
            public TimeSpan Timeout { get; set; }
        }

        public MemoryMessageQueue(string name = null)
            : base(name)
        {
            Name = name;
            Kind = "memory";
            Capabilities = new MessagingCapabilities(true, true, true, true, true, true, true, false, true);
        }

        public override bool IsOpened()
        {
            return _opened;
        }

        public async override Task OpenAsync(string correlationId, ConnectionParams connection, CredentialParams credential)
        {
            _opened = true;
            await Task.Delay(0);
        }

        public override async Task CloseAsync(string correlationId)
        {
            _opened = false;

            _cancel.Cancel();
            _receiveEvent.Set();

            _logger.Trace(correlationId, "Closed queue {0}", this);

            await Task.Delay(0);
        }

        public override long? MessageCount
        {
            get
            {
                lock (_lock)
                {
                    return _messages.Count;
                }
            }
        }

        public override async Task SendAsync(string correlationId, MessageEnvelop message)
        {
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
            _logger.Debug(message.CorrelationId, "Sent message {0} via {1}", message, this);
        }

        public override async Task<MessageEnvelop> PeekAsync(string correlationId)
        {
            MessageEnvelop message = null;

            lock (_lock)
            {
                // Pick a message
                if (_messages.Count > 0)
                    message = _messages.Peek();
            }

            if (message != null)
            {
                _logger.Trace(message.CorrelationId, "Peeked message {0} on {1}", message, this);
            }

            return await Task.FromResult(message);
        }

        public override async Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount)
        {
            List<MessageEnvelop> messages = null;

            lock (_lock)
            {
                messages = _messages.ToArray().Take(messageCount).ToList();
            }

            _logger.Trace(correlationId, "Peeked {0} messages on {1}", messages.Count, this);

            return await Task.FromResult(messages);
        }


        public override async Task<MessageEnvelop> ReceiveAsync(string correlationId, long waitTimeout)
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
                _counters.IncrementOne("queue." + Name + ".received_messages");
                _logger.Debug(message.CorrelationId, "Received message {0} via {1}", message, this);
            }

            return message;
        }

        public override async Task RenewLockAsync(MessageEnvelop message, long lockTimeout)
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

        public override async Task AbandonAsync(MessageEnvelop message)
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

        public override async Task CompleteAsync(MessageEnvelop message)
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

        public override async Task MoveToDeadLetterAsync(MessageEnvelop message)
        {
            if (message.Reference == null) return;

            lock (_lock)
            {
                int lockKey = (int)message.Reference;
                _lockedMessages.Remove(lockKey);
                message.Reference = null;
            }

            _counters.IncrementOne("queue." + Name + ".dead_messages");
            _logger.Trace(message.CorrelationId, "Moved to dead message {0} at {1}", message, this);

            await Task.Delay(0);
        }

        public override async Task ListenAsync(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback)
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
                            await callback(message, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(correlationId, ex, "Failed to process the message");
                        //await AbandonAsync(message);
                    }
                }
            }
        }

        public override void EndListen(string correlationId)
        {
            _cancel.Cancel();
        }

        public override async Task ClearAsync(string correlationId)
        {
            lock (_lock)
            {
                // Clear messages
                _messages.Clear();
                _lockedMessages.Clear();
            }

            _logger.Trace(correlationId, "Cleared queue {0}", this);

            await Task.Delay(0);
        }
    }
}
