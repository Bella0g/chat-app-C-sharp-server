using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace chat_app_shared_c_
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int UserId { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<string> Message { get; set; } = new List<string>();
    }
}