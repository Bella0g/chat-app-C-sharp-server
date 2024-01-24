namespace chat_app_shared_c_
{
    public class User
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<string> Message { get; set; } = new List<string>();
    }
}
