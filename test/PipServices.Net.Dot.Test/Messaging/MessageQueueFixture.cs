using System;
using PipServices.Net.Messaging;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Errors;
using Xunit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipServices.Net.Test.Messaging
{
    public sealed class MessageQueueFixture
    {
        private readonly IMessageQueue _queue;

        public MessageQueueFixture(IMessageQueue queue)
        {
            _queue = queue;
        }

        public void TestSendReceiveMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            _queue.Send(null, envelop1);

            var count = _queue.MessageCount;
            Assert.True(count > 0);

            var envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        private void Run(MessageEnvelop envelop)
        {
            //try
            //{
            Thread.Sleep(200);
            _queue.Send(null, envelop);
            //}
            //catch (InterruptedException ex)
            //{
            //    // Ignore...
            //}
        }

        public void TestReceiveSendMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");

            var task = Task.Run(() => Run(envelop1));
            task.Wait();

            var envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public void TestMoveToDeadMessage()
        {
            MessageEnvelop envelop1 = new MessageEnvelop("123", "Test", "Test message");
            _queue.Send(null, envelop1);

            var envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            _queue.MoveToDeadLetter(envelop2);
        }

        public void TestReceiveAndCompleteMessage()
        {
            MessageEnvelop envelop1 = new MessageEnvelop("123", "Test", "Test message");
            _queue.Send(null, envelop1);

            var envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            _queue.Complete(envelop2);
            //envelop2 = _queue.peek(null);
            //assertNull(envelop2);
        }

        public void TestReceiveAndAbandonMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            _queue.Send(null, envelop1);

            var envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);

            _queue.Abandon(envelop2);

            envelop2 = _queue.Receive(null, 10000);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public void TestSendPeekMessage()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            _queue.Send(null, envelop1);

            //try
            //{
            Thread.Sleep(200);
            //}
            //catch (InterruptedException ex)
            //{
            //    // Ignore...
            //}

            var envelop2 = _queue.Peek(null);
            Assert.NotNull(envelop2);
            Assert.Equal(envelop1.MessageType, envelop2.MessageType);
            Assert.Equal(envelop1.Message, envelop2.Message);
            Assert.Equal(envelop1.CorrelationId, envelop2.CorrelationId);
        }

        public void TestPeekNoMessage()
        {
            var envelop = _queue.Peek(null);
            Assert.Null(envelop);
        }

        private class TestMessageReceiver: IMessageReceiver
        {
            private readonly MessageEnvelop _envelop;

            public TestMessageReceiver(MessageEnvelop envelop)
            {
                _envelop = envelop;
            }

            public void ReceiveMessage(MessageEnvelop envelop, IMessageQueue queue)
            {
                _envelop.MessageId = envelop.MessageId;
                _envelop.CorrelationId = envelop.CorrelationId;
                _envelop.MessageType = envelop.MessageType;
                _envelop.Message = envelop.Message;
            }
        }

        public void TestListen()
        {
            var envelop1 = new MessageEnvelop("123", "Test", "Test message");
            var envelop2 = new MessageEnvelop();

            _queue.BeginListen(null, new TestMessageReceiver(envelop2));

            _queue.Send(null, envelop1);

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
