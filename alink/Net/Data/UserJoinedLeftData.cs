namespace alink.Net.Data
{
    public class UserJoinedLeftData
    {
        public UserInfo User { get; private set; }
        public bool Joined { get; private set; }

        public UserJoinedLeftData(UserInfo user, bool joined)
        {
            User = user;
            Joined = joined;
        }
    }
}
