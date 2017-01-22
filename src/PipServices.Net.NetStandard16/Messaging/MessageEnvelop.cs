using PipServices.Commons.Convert;
using PipServices.Commons.Data;
using System;
using System.Runtime.Serialization;
using System.Text;

namespace PipServices.Net.Messaging
{
    [DataContract]
    public class MessageEnvelop
    {
        public MessageEnvelop() { }

        public MessageEnvelop(string correlationId, string messageType, string message)
        {
            CorrelationId = correlationId;
            MessageType = messageType;
            Message = message;
            MessageId = IdGenerator.NextLong();
        }

        public MessageEnvelop(string correlationId, string messageType, object message)
        {
            CorrelationId = correlationId;
            MessageType = messageType;
            SetMessage(message);
            MessageId = IdGenerator.NextLong();
        }

        [IgnoreDataMember]
        public object Reference { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        [DataMember]
        public string MessageId { get; set; }

        [DataMember]
        public string MessageType { get; set; }

        [DataMember]
        public DateTime SentTimeUtc { get; set; }

        [DataMember]
        public string Message { get; set; }

        public void SetMessage(object message)
        {
            Message = JsonConverter.ToJson(message);
        }

        public T GetMessage<T>()
        {
            return JsonConverter.FromJson<T>(Message);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[');
            builder.Append(CorrelationId ?? "---");
            builder.Append(',');
            builder.Append(MessageType ?? "---");
            builder.Append(',');
            var sample = Message ?? "---";
            sample = sample.Length > 150 ? sample.Substring(0, 150) : sample;
            builder.Append(sample);
            builder.Append(']');
            return builder.ToString();
        }
    }
}