using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ChatApp.Shared
{
    public class Message : IComparable<Message>
    {
        [BsonId]
        public string Id { get; set; }

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

        public int CompareTo(Message other)
        {
            return time.CompareTo(other.time);
        }

        public override string ToString()
        {
            return $"{username}: {content}";
        }
    }
}
