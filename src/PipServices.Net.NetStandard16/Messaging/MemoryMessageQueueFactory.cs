using PipServices.Commons.Build;
using PipServices.Commons.Refer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipServices.Net.Messaging
{
    public class MemoryMessageQueueFactory : IFactory
    {
        public static readonly Descriptor Descriptor = new Descriptor("pip-services-net", "factory", "message-queue", "memory", "1.0");
        public static readonly Descriptor QueueDescriptor = new Descriptor("*", "message-queue", "memory", "*", "*");

        public MemoryMessageQueueFactory() { }

        public Descriptor GetDescriptor()
        {
            return Descriptor;
        }

        public bool CanCreate(object locator)
        {
            var descriptor = locator as Descriptor;
            if (descriptor == null || !descriptor.Match(QueueDescriptor))
                return false;
            return true;
        }

        public object Create(object locator)
        {
            var descriptor = locator as Descriptor;
            if (descriptor == null || !descriptor.Match(QueueDescriptor))
                throw new CreateException(null, locator);

            return new MemoryMessageQueue(descriptor.Name);
        }
    }
}
