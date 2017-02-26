using Xunit;

namespace PipServices.Net.Messaging
{
    public class MemoryMessageQueueTest
    {
        MemoryMessageQueue Queue { get; set; }
        MessageQueueFixture Fixture { get; set; }

        public MemoryMessageQueueTest()
        {
            Queue = new MemoryMessageQueue("TestQueue");
            //Queue.SetReferences(new MockReferences());
            Queue.OpenAsync(null).Wait();

            Fixture = new MessageQueueFixture(Queue);
        }

        [Fact]
        public void TestMockSendReceiveMessage()
        {
            Fixture.TestSendReceiveMessageAsync().Wait();
        }

        [Fact]
        public void TestMockReceiveSendMessage()
        {
            Fixture.TestReceiveSendMessageAsync().Wait();
        }

        [Fact]
        public void TestMockReceiveAndComplete()
        {
            Fixture.TestReceiveAndCompleteMessageAsync().Wait();
        }

        [Fact]
        public void TestMockReceiveAndAbandon()
        {
            Fixture.TestReceiveAndAbandonMessageAsync().Wait();
        }

        [Fact]
        public void TestMockSendPeekMessage()
        {
            Fixture.TestSendPeekMessageAsync().Wait();
        }

        [Fact]
        public void TestMockPeekNoMessage()
        {
            Fixture.TestPeekNoMessageAsync().Wait();
        }

        [Fact]
        public void TestMockOnMessage()
        {
            Fixture.TestOnMessageAsync().Wait();
        }

        [Fact]
        public void TestMockMoveToDeadMessage()
        {
            Fixture.TestMoveToDeadMessageAsync().Wait();
        }

        [Fact]
        public void TestMessageCount()
        {
            Fixture.TestMessageCountAsync().Wait();
        }
    }
}
