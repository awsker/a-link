namespace alink.Models
{
    public class NetSettings
    {
        public string Address { get; set; }
        public int JoinPort { get; set; }
        public int HostPort { get; set; }
        
        public string Nickname { get; set; }

        public NetSettings (string addr, int joinport, int hostport, string nick)
        {
            Address = addr;
            JoinPort = joinport;
            HostPort = hostport;
            Nickname = nick;
        }

        public NetSettings(string settings)
        {
            var split = settings.Split('|');
            Address = split[0];
            JoinPort = int.Parse(split[1]);
            HostPort = int.Parse(split[2]);
            Nickname = split[3];
        }

        public string Serialize()
        {
            return string.Join("|", Address, JoinPort, HostPort, Nickname);
        }
    }
}
