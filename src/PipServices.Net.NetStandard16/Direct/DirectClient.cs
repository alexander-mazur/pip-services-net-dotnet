using System;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;

namespace PipServices.Net.Direct
{
    public abstract class DirectClient : IOpenable, IReferenceable
    {
        protected CompositeLogger _logger = new CompositeLogger();
        protected CompositeCounters _counters = new CompositeCounters();

        public Task CloseAsync(string correlationId)
        {
            if (IsOpened())
                _logger.Debug(correlationId, "Closed Direct client {0}", this.GetType().Name);

            return Task.Delay(0);
        }

        public abstract bool IsOpened();

        public Task OpenAsync(string correlationId)
        {
            if (IsOpened())
                return Task.Delay(0);

            _logger.Info(correlationId, "Opened Direct client {0}", this.GetType().Name);

            return Task.Delay(0);
        }

        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
        }

        bool IOpenable.IsOpened()
        {
            return IsOpened();
        }

        protected Timing Instrument(string correlationId, string name)
        {
            _logger.Trace(correlationId, "Calling {0} method", name);
            return _counters.BeginTiming(name + ".call_time");
        }
    }
}
