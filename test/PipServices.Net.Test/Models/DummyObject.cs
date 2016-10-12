using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using PipServices.Commons.Data;

namespace PipServices.Net.Test.Models
{
    [DataContract]
    [JsonObject(MemberSerialization.OptIn)]
    [BsonIgnoreExtraElements]
    public class DummyObject : IIdentifiable<string>
    {
        [BsonId]
        [BsonIgnoreIfNull]
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DataMember(Name = "id", IsRequired = true)]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("key")]
        [JsonProperty("key", DefaultValueHandling = DefaultValueHandling.Include)]
        [DataMember(Name = "key", IsRequired = true)]
        public string Key { get; set; }

        [BsonRequired]
        [BsonElement("content")]
        [JsonProperty("content", DefaultValueHandling = DefaultValueHandling.Include)]
        [DataMember(Name = "content", IsRequired = false)]
        public string Content { get; set; }
    }
}
