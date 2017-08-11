using System;

namespace alink.Net.Data
{
    public class UserInfo
    {
        public string Username { get; set; }
        public bool Approved { get; set; }
        public Guid Guid { get; set; }
        
        public UserInfo(string username, Guid guid)
        {
            Username = username;
            Guid = guid;
            Approved = true;
        }
    }
}
