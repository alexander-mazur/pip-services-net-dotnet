using Xunit;

namespace PipServices.Net.Messaging
{
    public sealed class MemoryMessageQueueTest
    {
        private readonly MemoryMessageQueue _queue;
        private readonly MessageQueueFixture _fixture;

        public MemoryMessageQueueTest()
        {
            _queue = new MemoryMessageQueue("test");
            _fixture = new MessageQueueFixture(_queue);

            var clearTask = _queue.ClearAsync(null);
            clearTask.Wait();

            var openTask = _queue.OpenAsync(null);
            openTask.Wait();
        }

        [Fact]
        public void TestSendReceiveMessage()
        {
            var task = _fixture.TestSendReceiveMessage();
            task.Wait();
        }

        [Fact]
        public void TestReceiveSendMessage()
        {
            var task = _fixture.TestReceiveSendMessage();
            task.Wait();
        }

        [Fact]
        public void TestMoveToDeadMessage()
        {
            var task = _fixture.TestMoveToDeadMessage();
            task.Wait();
        }

        [Fact]
        public void TestReceiveAndCompleteMessage()
        {
            var task = _fixture.TestReceiveAndCompleteMessage();
            task.Wait();
        }

        [Fact]
        public void TestReceiveAndAbandonMessage()
        {
            var task = _fixture.TestReceiveAndAbandonMessage();
            task.Wait();
        }

        [Fact]
        public void TestSendPeekMessage()
        {
            var task = _fixture.TestSendPeekMessage();
            task.Wait();
        }

        [Fact]
        public void TestPeekNoMessage()
        {
            var task = _fixture.TestPeekNoMessage();
            task.Wait();
        }

        [Fact]
        public void TestListen()
        {
            var task = _fixture.TestListen();
            task.Wait();
        }
    }
}
