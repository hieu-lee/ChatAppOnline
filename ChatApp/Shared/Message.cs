using MongoDB.Bson;
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
        public ObjectId Id { get; set; }

        [BsonElement("username")]
        public string username { get; set; }

        [BsonElement("content")]
        public string content { get; set; }

        [BsonElement("time")]
        public DateTime time { get; set; } = DateTime.Now;

        [BsonElement("received")]
        public bool received { get; set; } = false;

        [BsonElement("image")]
        public byte[] image { get; set; }

        public override string ToString()
        {
            return $"{username}: {content}";
        }
    }
}
