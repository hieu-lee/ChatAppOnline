using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace ChatApp.Shared
{
    public class Account
    {
        [BsonId]
        public string username { get; set; }
        [BsonElement("email")]
        public string email { get; set; }
        [BsonElement("password")]
        public string password { get; set; }
        [BsonElement("connected")]
        public bool connected { get; set; }
        [BsonElement("typing")]
        public bool typing { get; set; } = false;
        [BsonElement("contacts")]
        public HashSet<string> contacts = new HashSet<string>();
        [BsonElement("avatar")]
        public byte[] avatar { get; set; }
        [BsonElement("rooms")]
        public HashSet<string> rooms { get; set; } = new HashSet<string>();
    }
}
