using System;
using Newtonsoft.Json;
using PipServices.Commons.Data;

namespace PipServices.Net.Test
{
    public class Dummy : IStringIdentifiable
    {
        public Dummy()
        {
        }

        public Dummy(string id, string key, string content)
        {
            Id = id;
            Key = key;
            Content = content;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
