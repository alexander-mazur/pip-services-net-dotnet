namespace PipServices.Net.Messaging
{
    public class MessagingCapabilities
    {
        public MessagingCapabilities(
            bool canMessageCount, bool canSend, bool canReceive, bool canPeek, bool canPeekBatch,
            bool canRenewLock, bool canAbandon, bool canDeadLetter, bool canClear
        )
        {
            CanMessageCount = canMessageCount;
            CanSend = canSend;
            CanReceive = canReceive;
            CanPeek = canPeek;
            CanPeekBatch = canPeekBatch;
            CanRenewLock = canRenewLock;
            CanAbandon = canAbandon;
            CanDeadLetter = canDeadLetter;
            CanClear = canClear;
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