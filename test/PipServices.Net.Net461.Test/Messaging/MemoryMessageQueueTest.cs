using System;
using PipServices.Net.Messaging;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Errors;
using Xunit;
using System.Linq;
using System.Threading;

namespace PipServices.Net.Test.Messaging
{
    public sealed class MemoryMessageQueueTest
    {
        private readonly MemoryMessageQueue _queue;
        private readonly MessageQueueFixture _fixture;

        public MemoryMessageQueueTest()
        {
            _queue = new MemoryMessageQueue("test");
            _fixture = new MessageQueueFixture(_queue);

            var clearTask = _queue.ClearAsync(null, CancellationToken.None);
            clearTask.Wait();

            var openTask = _queue.OpenAsync(null, CancellationToken.None);
            openTask.Wait();
        }

        [Fact]
        public void TestSendReceiveMessage()
        {
            _fixture.TestSendReceiveMessage();
        }

        [Fact]
        public void TestReceiveSendMessage()
        {
            _fixture.TestReceiveSendMessage();
        }

        [Fact]
        public void TestMoveToDeadMessage()
        {
            _fixture.TestMoveToDeadMessage();
        }

        [Fact]
        public void TestReceiveAndCompleteMessage()
        {
            _fixture.TestReceiveAndCompleteMessage();
        }

        [Fact]
        public void TestReceiveAndAbandonMessage()
        {
            _fixture.TestReceiveAndAbandonMessage();
        }

        [Fact]
        public void TestSendPeekMessage()
        {
            _fixture.TestSendPeekMessage();
        }

        [Fact]
        public void TestPeekNoMessage()
        {
            _fixture.TestPeekNoMessage();
        }

        [Fact]
        public void TestListen()
        {
            _fixture.TestListen();
        }
    }
}
