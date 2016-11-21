using System;
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
        private readonly IList<MessageEnvelop> _messages = new List<MessageEnvelop>();
        private int _lockTokenSequence;
        private readonly IDictionary<int, LockedMessage> _lockedMessages = new Dictionary<int, LockedMessage>();
        private bool _listening;
        private readonly CompositeLogger _logger = new CompositeLogger();
        private readonly CompositeCounters _counters = new CompositeCounters();

        private class LockedMessage
        {
            //public MessageEnvelop message;
            public long LockExpiration;
        }

        public MemoryMessageQueue()
        {
            Capabilities = new MessagingCapabilities(
                true, true, true, true, true, true, true, false, true);
        }

        public MemoryMessageQueue(string name)
            : this()
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
                _name = descriptor.Id;
            }
        }

        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
        }

        public Task OpenAsync(string correlationId)
        {
            _logger.Trace(correlationId, "Opened queue %s", this);

            return Task.Delay(0);
        }

        public Task CloseAsync(string correlationId)
        {
            lock(_lock) {
                _listening = false;
            }

            _logger.Trace(correlationId, "Closed queue %s", this);

            return Task.Delay(0);
        }

        public string Name => _name ?? "undefined";

        public MessagingCapabilities Capabilities { get; }

        public long MessageCount
        {
            get
            {
                lock(_lock) {
                    return _messages.Count;
                }
            }
        }

        public void Send(string correlationId, MessageEnvelop message)
        {
            if (message == null) return;

            lock (_lock) {
                // Add message to the queue
                _messages.Add(message);
            }

            _counters.IncrementOne("queue." + Name + ".sent_messages");
            _logger.Debug(correlationId, "Sent message %s via %s", message, this);
        }

        public void SendAsObject(string correlationId, string messageType, object message)
        {
            var envelop = new MessageEnvelop(correlationId, messageType, message);
            Send(correlationId, envelop);
        }

        public MessageEnvelop Peek(string correlationId)
        {
            MessageEnvelop message = null;

            lock(_lock) {
                // Pick a message
                if (_messages.Count > 0)
                    message = _messages[0];
            }

            if (message != null)
                _logger.Trace(correlationId, "Peeked message %s on %s", message, this);

            return message;
        }

        public IEnumerable<MessageEnvelop> PeekBatch(string correlationId, int messageCount)
        {
            var messages = new List<MessageEnvelop>();

            lock(_lock) {
                for (var index = 0; index < _messages.Count && index < messageCount; index++)
                    messages.Add(_messages[index]);
            }

            _logger.Trace(correlationId, "Peeked %d messages on %s", messages.Count, this);

            return messages;
        }


        public MessageEnvelop Receive(string correlationId, long waitTimeout)
        {
            MessageEnvelop message = null;

            lock(_lock) {
                // Try to get a message
                if (_messages.Count > 0)
                {
                    message = _messages[0];
                    _messages.RemoveAt(0);
                }

                if (message == null)
                {
                    Thread.Sleep(TimeSpan.FromTicks(waitTimeout));
                //    try
                //    {
                //        _lock.wait(waitTimeout);
                //    }
                //    catch (InterruptedException ex)
                //    {
                //        return null;
                //    }
                }

                // Try to get a message again
                if (message == null && _messages.Count > 0)
                {
                    message = _messages[0];
                    _messages.RemoveAt(0);
                }

                // Exit if message was not found
                if (message == null)
                    return null;

                // Generate and set locked token
                var lockedToken = _lockTokenSequence++;
                message.Reference = lockedToken;

                // Add messages to locked messages list
                var lockedMessage = new LockedMessage
                {
                    LockExpiration = Environment.TickCount + _defaultLockTimeout
                };
                //lockedMessage.message = message;

                _lockedMessages[lockedToken] = lockedMessage;
            }

            _counters.IncrementOne("queue." + Name + ".received_messages");
            _logger.Debug(message.CorrelationId, "Received message %s via %s", message, this);

            return message;
        }

        public void RenewLock(MessageEnvelop message, long lockTimeout)
        {
            if (message?.Reference == null)
                return;

            lock(_lock) {
                // Get message from locked queue
                var lockedToken = (int)message.Reference;
                var lockedMessage = _lockedMessages[lockedToken];

                // If lock is found, extend the lock
                if (lockedMessage != null)
                    lockedMessage.LockExpiration = Environment.TickCount + lockTimeout;
            }

            _logger.Trace(message.CorrelationId, "Renewed lock for message %s at %s", message, this);
        }

        public void Abandon(MessageEnvelop message)
        {
            if (message?.Reference == null)
                return;

            lock(_lock) {
                // Get message from locked queue
                var lockedToken = (int)message.Reference;
                var lockedMessage = _lockedMessages[lockedToken];
                if (lockedMessage != null)
                {
                    // Remove from locked messages
                    _lockedMessages.Remove(lockedToken);
                    message.Reference = null;

                    // Skip if it is already expired
                    if (lockedMessage.LockExpiration <= Environment.TickCount)
                        return;
                }
                // Skip if it absent
                else return;
            }

            _logger.Trace(message.CorrelationId, "Abandoned message %s at %s", message, this);

            // Add back to the queue
            Send(message.CorrelationId, message);
        }

        public void Complete(MessageEnvelop message)
        {
            if (message?.Reference == null)
                return;

            lock(_lock) {
                var lockKey = (int)message.Reference;
                _lockedMessages.Remove(lockKey);
                message.Reference = null;
            }

            _logger.Trace(message.CorrelationId, "Completed message %s at %s", message, this);
        }

        public void MoveToDeadLetter(MessageEnvelop message)
        {
            if (message?.Reference == null)
                return;

            lock(_lock) {
                var lockKey = (int)message.Reference;
                _lockedMessages.Remove(lockKey);
                message.Reference = null;
            }

            _counters.IncrementOne("queue." + Name + ".dead_messages");
            _logger.Trace(message.CorrelationId, "Moved to dead message %s at %s", message, this);
        }

        public void Listen(string correlationId, IMessageReceiver receiver)
        {
            if (_listening)
            {
                _logger.Error(correlationId, "Already listening queue %s", this);
                return;
            }

            _logger.Trace(correlationId, "Started listening messages at %s", this);

            _listening = true;

            while (_listening)
            {
                var message = Receive(correlationId, _defaultWaitTimeout);

                if (_listening && message != null)
                {
                    try
                    {
                        receiver.ReceiveMessage(message, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(correlationId, ex, "Failed to process the message");
                        //await AbandonAsync(message);
                    }
                }
            }

            _logger.Trace(correlationId, "Stopped listening messages at %s", this);
        }

        public void BeginListen(string correlationId, IMessageReceiver receiver)
        {
            // Start listening on a parallel tread
            var thread = new Thread(() => Listen(correlationId, receiver));
            thread.Start();
        }

        public void EndListen(string correlationId)
        {
            _listening = false;
        }

        public Task ClearAsync(string correlationId)
        {
            lock (_lock)
            {
                // Clear messages
                _messages.Clear();
                _lockedMessages.Clear();
            }

            _logger.Trace(correlationId, "Cleared queue %s", this);

            return Task.Delay(0);
        }

        public override string ToString()
        {
            return "[" + Name + "]";
        }
    }
}
