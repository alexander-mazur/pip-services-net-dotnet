using System.Text;
using Newtonsoft.Json;
using PipServices.Commons.Convert;
using PipServices.Commons.Data;

namespace PipServices.Net.Messaging
{
    public sealed class MessageEnvelop
    {
        public MessageEnvelop()
        {
        }

        public MessageEnvelop(string correlationId, string messageType, object message)
        {
            CorrelationId = correlationId;
            MessageType = messageType;
            Message = message;
            MessageId = IdGenerator.NextLong();
        }

        [JsonIgnore]
        public object Reference { get; set; }

        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("message_type")]
        public string MessageType { get; set; }

        [JsonProperty("message")]
        public object Message { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder()
                .Append("[")
                .Append(string.IsNullOrWhiteSpace(CorrelationId) ? "---" : CorrelationId)
                .Append(",")
                .Append(string.IsNullOrWhiteSpace(MessageType) ? "---" : MessageType)
                .Append(",")
                .Append(Message != null ? StringConverter.ToString(Message) : "--")
                .Append("]");
            return builder.ToString();
        }
    }
}