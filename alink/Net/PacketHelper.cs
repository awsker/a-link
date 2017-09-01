using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace alink.Net
{
    public static class PacketHelper
    {
        public static byte[] ConcatBytes(params byte[][] arrays)
        {
            var totalArraySize = arrays.Sum(a => a.Length);
            byte[] buffer = new byte[totalArraySize];
            int current = 0;
            foreach (var a in arrays)
            {
                a.CopyTo(buffer, current);
                current += a.Length;
            }
            return buffer;
        }

        public static byte[] ConcatPackets(IList<NetSerializable> stuff)
        {
            byte[][] data = new byte[stuff.Count + 1][];
            data[0] = BitConverter.GetBytes(stuff.Count);
            for (int i = 0; i < stuff.Count; ++i)
            {
                data[i + 1] = stuff[i].GetBytes();
            }
            return ConcatBytes(data);
        }

        public static byte[] GetStringBytes(string text)
        {
            var textBytes = Encoding.Unicode.GetBytes(text);
            return ConcatBytes(BitConverter.GetBytes(textBytes.Length), textBytes);
        }

        public static string GetStringFromBytes(byte[] buffer, int offset, out int newOffset)
        {
            var numBytes = BitConverter.ToInt32(buffer, offset);
            newOffset = offset + 4 + numBytes;
            return Encoding.Unicode.GetString(buffer, offset + 4, numBytes);
        }

        public static Packet CreateServerRegistrationPacket(Guid clientGuid)
        {
            var data = ConcatBytes(GetStringBytes(NetConstants.ServerRegisterString), clientGuid.ToByteArray());
            return new Packet(NetConstants.PacketTypes.ServerRegister, data);
        }

        public static Packet CreateUserRegistrationPacket(string nick)
        {
            var data1 = GetStringBytes(NetConstants.UserRegisterString);
            var data2 = GetStringBytes(nick);
            return new Packet(NetConstants.PacketTypes.UserRegister, ConcatBytes(data1, data2));
        }

        public static Packet CreateUserJoinedPacket(UserThread user)
        {
            return new Packet(NetConstants.PacketTypes.UserJoined, user.GetBytes());
        }

        public static Packet CreateUserLeftPacket(UserThread user)
        {
            return new Packet(NetConstants.PacketTypes.UserLeft, user.GetBytes());
        }

        public static Packet CreateUsersAlreadyHerePacket(IList<UserThread> users)
        {
            return new Packet(NetConstants.PacketTypes.UserAlreadyHere, ConcatPackets(users.Cast<NetSerializable>().ToList()));
        }

        public static Packet CreateUserToServerChatMessage(string text)
        {
            var data = GetStringBytes(text);
            return new Packet(NetConstants.PacketTypes.UserChat, data);
        }

        public static Packet CreateServerToUserChatMessage(UserThread sender, string text)
        {
            var data = ConcatBytes(sender.GetBytes(), GetStringBytes(text));
            return new Packet(NetConstants.PacketTypes.ServerToUserChat, data);
        }
    }
}
