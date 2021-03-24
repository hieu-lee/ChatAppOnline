using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Shared
{
    public class Message
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
        public bool typing { get; set; }
        [BsonElement("contacts")]
        public HashSet<string> contacts = new HashSet<string>();
        [BsonElement("avatar")]
        public byte[] avatar { get; set; }
        [BsonElement("rooms")]
        public HashSet<string> rooms { get; set; } = new HashSet<string>();
    }
}
