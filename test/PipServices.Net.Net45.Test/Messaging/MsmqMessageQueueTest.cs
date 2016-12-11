//using PipServices.Commons.Config;
//using PipServices.Commons.Connect;
//using System.Threading.Tasks;
//using Xunit;

//namespace PipServices.Net.Messaging
//{
//    public class MsmqMessageQueueTest
//    {
//        MsmqMessageQueue Queue { get; set; }
//        MessageQueueFixture Fixture { get; set; }

//        public MsmqMessageQueueTest()
//        {
//            var config = YamlConfigReader.ReadConfig(null, "..\\..\\..\\config\\test_connections.yaml");
//            var connection = ConnectionParams.FromString(config.GetAsString("msmq_queue"));

//            Queue = new MsmqMessageQueue("TestQueue", connection);
//            //Queue.SetReferences(new MockReferences());
//            Queue.Interval = 50;
//            Queue.OpenAsync(null).Wait();

//            Fixture = new MessageQueueFixture(Queue);
//        }

//        [Fact]
//        public async Task TestMsmqSendReceiveMessageAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestSendReceiveMessageAsync();
//        }

//        //[Fact]
//        //public async Task TestMsmqReceiveSendMessageAsync()
//        //{
//        //    await Queue.ClearAsync(null);
//        //    await Fixture.TestReceiveSendMessageAsync();
//        //}

//        [Fact]
//        public async Task TestMsmqReceiveAndCompleteAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestReceiveAndCompleteMessageAsync();
//        }

//        [Fact]
//        public async Task TestMsmqReceiveAndAbandonAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestReceiveAndAbandonMessageAsync();
//        }

//        [Fact]
//        public async Task TestMsmqSendPeekMessageAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestSendPeekMessageAsync();
//        }

//        [Fact]
//        public async Task TestMsmqPeekNoMessageAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestPeekNoMessageAsync();
//        }

//        [Fact]
//        public async Task TestMsmqOnMessageAsync()
//        {
//            await Queue.ClearAsync(null);
//            await Fixture.TestOnMessageAsync();
//        }

//        //[Fact]
//        //public async Task TestMsmqMoveToDeadMessageAsync()
//        //{
//        //    await Fixture.TestMoveToDeadMessageAsync();
//        //}

//        //[Fact]
//        //public async Task TestMsmqNullMessageAsync()
//        //{
//        //    var envelop = await Queue.ReceiveAsync(TimeSpan.FromMilliseconds(10000000));
//        //    await Queue.CompleteAsync(envelop);
//        //    Assert.IsNotNull(envelop);
//        //}
//    }
//}
