namespace alink.Net
{
    public class NetConstants
    {
        public const string ServerRegisterString = "a-link server";
        public const string UserRegisterString = "hello";

        public enum PacketTypes
        {
            ServerRegister,
            UserRegister,
            UserJoined,
            UserAlreadyHere,
            UserLeft,
            UserChat,
            ServerToUserChat,
            ServerRulesConfigChecksum,
            ServerRulesConfigData,
            ServerShutdown,
            MemoryData
        }
    }
}
