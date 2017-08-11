namespace alink.Net.Data
{
    public class ChatData
    {
        public UserInfo User { get; private set; }
        public string Message { get; private set; }

        public ChatData(UserInfo user, string message)
        {
            User = user;
            Message = message;
        }
    }
}
