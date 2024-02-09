using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Users
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<string> Message { get; set; } = new List<string>();
    }
}