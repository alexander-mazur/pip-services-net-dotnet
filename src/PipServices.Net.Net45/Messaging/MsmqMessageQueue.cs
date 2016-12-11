using PipServices.Commons.Auth;
using PipServices.Commons.Config;
using PipServices.Commons.Connect;
using PipServices.Commons.Convert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public class MsmqMessageQueue : MessageQueue
    {
        private long DefaultVisibilityTimeout = 60000;
        private long DefaultCheckInterval = 10000;

        private CancellationTokenSource _cancel = new CancellationTokenSource();

        public MsmqMessageQueue(string name = null)
        {
            Name = name;
            Capabilities = new MessagingCapabilities(false, true, true, true, true, false, true, true, true);
            Interval = DefaultCheckInterval;
        }

        public MsmqMessageQueue(string name, ConfigParams config)
            : this(name)
        {
            if (config != null) Configure(config);
        }

        public MsmqMessageQueue(string name, System.Messaging.MessageQueue queue)
            : this(name)
        {
            InnerQueue = queue;
        }

        public System.Messaging.MessageQueue InnerQueue { get; private set; }
        public long Interval { get; set; }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);

            Interval = config.GetAsLongWithDefault("interval", Interval);
        }

        public async override Task OpenAsync(string correlationId, ConnectionParams connection, CredentialParams credential)
        {
            var queuePath = connection.GetAsNullableString("path")
                ?? connection.GetAsNullableString("queue_path")
                ?? connection.GetAsNullableString("QueuePath")
                ?? Name;

            _logger.Info(null, "Connecting queue {0} to {1}", Name, queuePath);

            // Todo: Implement credential params
            if (System.Messaging.MessageQueue.Exists(queuePath) == false)
                System.Messaging.MessageQueue.Create(queuePath, true);

            InnerQueue = new System.Messaging.MessageQueue(queuePath);

            await Task.Delay(0);
        }

        public override async Task CloseAsync(string correlationId)
        {
            InnerQueue.Close();
            _cancel.Cancel();

            _logger.Trace(correlationId, "Closed queue {0}", this);

            await Task.Delay(0);
        }

        public override long? MessageCount
        {
            get
            {
                var messages = InnerQueue.GetAllMessages();
                return messages != null ? (long?)messages.Length : null;
            }
        }

        private MessageEnvelop ToMessage(Message envelop)
        {
            if (envelop == null) return null;

            MessageEnvelop message = null;

            try
            {
                envelop.Formatter = InnerQueue.Formatter;
                //var reader = new StreamReader(envelop.BodyStream);
                //string content = reader.ReadToEnd();
                string content = "" + envelop.Body;
                message = JsonConverter.FromJson<MessageEnvelop>(content);
            }
            catch
            {
                // Handle broken messages gracefully
                _logger.Warn(null, "Cannot deserialize message: " + envelop.Body);
            }

            // If message is broken or null
            if (message == null)
            {
                message = new MessageEnvelop
                {
                    MessageType = envelop.Label,
                    MessageId = envelop.Id,
                    SentTimeUtc = envelop.SentTime,
                    Message = JsonConverter.ToJson(envelop.Body)
                };
            }

            return message;
        }

        public override async Task SendAsync(string correlationId, MessageEnvelop message)
        {
            var envelop = new Message();
            if (message.MessageType != null)
                envelop.Label = message.MessageType;

            envelop.Formatter = InnerQueue.Formatter;
            envelop.Body = JsonConverter.ToJson(message);

            if (InnerQueue.Transactional)
            {
                var transaction = new MessageQueueTransaction();
                transaction.Begin();
                InnerQueue.Send(envelop, transaction);
                transaction.Commit();
            }
            else
            {
                InnerQueue.Send(envelop);
            }

            _counters.IncrementOne("queue." + Name + ".sent_messages");
            _logger.Debug(message.CorrelationId, "Sent message {0} via {1}", message, this);

            await Task.Delay(0);
        }

        public override async Task<MessageEnvelop> PeekAsync(string correlationId)
        {
            await Task.Delay(0);

            Message envelop = null;

            try
            {
                envelop = InnerQueue.Peek(TimeSpan.FromMilliseconds(0));
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                    throw ex;
            }

            if (envelop == null) return null;

            var message = ToMessage(envelop);

            if (message != null)
            {
                _logger.Trace(message.CorrelationId, "Peeked message {0} on {1}", message, this);
            }

            return message;
        }

        public override async Task<List<MessageEnvelop>> PeekBatchAsync(string correlationId, int messageCount)
        {
            var envelops = InnerQueue.GetAllMessages().Take(messageCount);
            var messages = new List<MessageEnvelop>();

            foreach (var envelop in envelops)
            {
                var message = ToMessage(envelop);
                if (message != null)
                    messages.Add(message);
            }

            _logger.Trace(correlationId, "Peeked {0} messages on {1}", messages.Count, this);

            return await Task.FromResult(messages);
        }


        public override async Task<MessageEnvelop> ReceiveAsync(string correlationId, long waitTimeout)
        {
            await Task.Delay(0);

            MessageQueueTransaction transaction = null;
            if (InnerQueue.Transactional)
            {
                transaction = new MessageQueueTransaction();
                transaction.Begin();
            }

            Message envelop = null;

            try
            {
                if (transaction != null)
                    envelop = InnerQueue.Receive(TimeSpan.FromMilliseconds(waitTimeout), transaction);
                else
                    envelop = InnerQueue.Receive(TimeSpan.FromMilliseconds(waitTimeout));
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.TransactionUsage)
                    transaction = null;
                else if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout)
                    throw ex;
            }

            if (envelop == null)
            {
                if (transaction != null)
                    transaction.Abort();
                return null;
            }

            var message = ToMessage(envelop);

            if (message != null)
            {
                message.Reference = transaction;

                _counters.IncrementOne("queue." + Name + ".received_messages");
                _logger.Debug(message.CorrelationId, "Received message {0} via {1}", message, this);
            }

            return message;
        }

        public override async Task RenewLockAsync(MessageEnvelop message, long lockTimeout)
        {
            // This feature is not supported
            await Task.Delay(0);
        }

        public override async Task AbandonAsync(MessageEnvelop message)
        {
            // Abort the transaction if it is present
            var transaction = (MessageQueueTransaction)message.Reference;
            if (transaction != null)
            {
                transaction.Abort();
                message.Reference = null;
                _logger.Trace(message.CorrelationId, "Abandoned message {0} at {1}", message, this);
            }
            await Task.Delay(0);
        }

        public override async Task CompleteAsync(MessageEnvelop message)
        {
            // Complete the transaction if it is present
            var transaction = (MessageQueueTransaction)message.Reference;
            if (transaction != null)
            {
                transaction.Abort();
                message.Reference = null;
                _logger.Trace(message.CorrelationId, "Completed message {0} at {1}", message, this);
            }
            await Task.Delay(0);
        }

        public override async Task MoveToDeadLetterAsync(MessageEnvelop message)
        {
            // Remove the message from the queue
            //await _queue.DeleteMessageAsync((CloudQueueMessage)message.Reference, _cancel.Token);
            message.Reference = null;
            //Counters.IncrementOne("Queue." + Name + ".DeadMessages");
            //Logger.Trace(message.CorrelationId, "Moved to dead message {0} at {1}", message, this);

            await Task.Delay(0);
        }

        public override async Task ListenAsync(string correlationId, Func<MessageEnvelop, IMessageQueue, Task> callback)
        {
            _logger.Debug(null, "Started listening messages at {0}", this);

            // Create new cancelation token
            _cancel = new CancellationTokenSource();

            while (!_cancel.IsCancellationRequested)
            {
                var message = await ReceiveAsync(correlationId, DefaultVisibilityTimeout);

                if (message != null && !_cancel.IsCancellationRequested)
                {
                    _counters.IncrementOne("queue." + Name + ".received_messages");
                    _logger.Debug(message.CorrelationId, "Received message {0} via {1}", message, this);

                    try
                    {
                        await callback(message, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(correlationId, ex, "Failed to process the message");
                        //throw ex;
                    }
                }
                else
                {
                    // If no messages received then wait
                    await Task.Delay(TimeSpan.FromMilliseconds(Interval));
                }
            }
        }

        public override void EndListen(string correlationId)
        {
            _cancel.Cancel();
        }

        public override async Task ClearAsync(string correlationId)
        {
            InnerQueue.Purge();

            _logger.Trace(correlationId, "Cleared queue {0}", this);

            await Task.Delay(0);
        }
    }
}
