using PipServices.Commons.Build;
using PipServices.Commons.Refer;

namespace PipServices.Net.Messaging
{
    public class MemoryMessageQueueFactory : Factory
    {
        public static readonly Descriptor Descriptor = new Descriptor("pip-services-net", "factory", "message-queue", "memory", "1.0");
        public static readonly Descriptor MemoryQueueDescriptor = new Descriptor("pip-services-net", "message-queue", "memory", "*", "*");

        public MemoryMessageQueueFactory()
        {
            Register(MemoryQueueDescriptor, (locator) => {
                Descriptor descritor = (Descriptor)locator;
                return new MemoryMessageQueue(descritor.Name);
            });
        }
    }
}
