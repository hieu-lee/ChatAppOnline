using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Shared
{
    public class Room
    {
        [BsonId]
        public string Id { get; set; }
        [BsonElement("name")]
        public string name { get; set; }
        [BsonElement("password")]
        public string password { get; set; }
        [BsonElement("state")]
        public bool state { get; set; } = false;
        [BsonElement("users")]
        public HashSet<string> users { get; set; } = new();
        public override bool Equals(object obj)
        {
            var otherRoom = (Room)obj;
            return Id.Equals(otherRoom.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
