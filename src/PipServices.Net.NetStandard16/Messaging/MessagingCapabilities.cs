namespace PipServices.Net.Messaging
{
    public sealed class MessagingCapabilities
    {
        public MessagingCapabilities(bool messageCount, bool send, bool receive,
            bool peek, bool peekBatch, bool renewLock, bool abandon,
            bool deadLetter, bool clear)
        {
            CanMessageCount = messageCount;
            CanSend = send;
            CanReceive = receive;
            CanPeek = peek;
            CanPeekBatch = peekBatch;
            CanRenewLock = renewLock;
            CanAbandon = abandon;
            CanDeadLetter = deadLetter;
            CanClear = clear;
        }

        public bool CanMessageCount { get; private set; }
        public bool CanSend { get; private set; }
        public bool CanReceive { get; private set; }
        public bool CanPeek { get; private set; }
        public bool CanPeekBatch { get; private set; }
        public bool CanRenewLock { get; private set; }
        public bool CanAbandon { get; private set; }
        public bool CanDeadLetter { get; private set; }
        public bool CanClear { get; private set; }
    }
}