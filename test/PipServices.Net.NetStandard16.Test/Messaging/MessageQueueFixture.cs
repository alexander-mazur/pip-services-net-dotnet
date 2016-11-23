using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PipServices.Net.Messaging
{
    public sealed class MessageQueueFixture
    {
        private readonly IMessageQueue _queue;

        public MessageQueueFixture(IMessageQueue queue)
        {
            _queue = queue;
        }

        public async Task TestSendReceiveMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            await _queue.SendAsync(null, envelop1);

            var count = _queue.MessageCount;
            Assert.True(count > 0);

            var envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        private async Task Run(MessageEnvelop envelop)
        {
            //try
            //{
            Thread.Sleep(200);
            await _queue.SendAsync(null, envelop);
            //}
            //catch (InterruptedException ex)
            //{
            //    // Ignore...
            //}
        }

        public async Task TestReceiveSendMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");

            await Task.Run(() => Run(envelop1));

            var envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public async Task TestMoveToDeadMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            await _queue.SendAsync(null, envelop1);

            var envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            await _queue.MoveToDeadLetterAsync(envelop2);
        }

        public async Task TestReceiveAndCompleteMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            await _queue.SendAsync(null, envelop1);

            var envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            await _queue.CompleteAsync(envelop2);
            //envelop2 = _queue.peek(null);
            //assertNull(envelop2);
        }

        public async Task TestReceiveAndAbandonMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            await _queue.SendAsync(null, envelop1);

            var envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            await _queue.AbandonAsync(envelop2);

            envelop2 = await _queue.ReceiveAsync(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public async Task TestSendPeekMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            await _queue.SendAsync(null, envelop1);

            //try
            //{
            Thread.Sleep(200);
            //}
            //catch (InterruptedException ex)
            //{
            //    // Ignore...
            //}

            var envelop2 = await _queue.PeekAsync(null);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public async Task TestPeekNoMessage()
        {
            var envelop = await _queue.PeekAsync(null);
            Assert.Null(envelop);
        }

        private class TestMessageReceiver: IMessageReceiver
        {
            private readonly MessageEnvelop _envelop;

            public TestMessageReceiver(MessageEnvelop envelop)
            {
                _envelop = envelop;
            }

            public Task ReceiveMessageAsync(MessageEnvelop envelop, IMessageQueue queue)
            {
                _envelop.MessageId = envelop.MessageId;
                _envelop.CorrelationId = envelop.CorrelationId;
                _envelop.MessageType = envelop.MessageType;
                _envelop.Message = envelop.Message;

                return Task.Delay(0);
            }
        }

        public async Task TestListen()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            var envelop2 = new MessageEnvelop();

            _queue.BeginListen(null, new TestMessageReceiver(envelop2));

            await _queue.SendAsync(null, envelop1);

            //try
            //{
                Thread.Sleep(200);
            //}
            //catch (InterruptedException ex)
            //{
            //    // Ignore...
            //}

            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            _queue.EndListen(null);
        }
    }
}
