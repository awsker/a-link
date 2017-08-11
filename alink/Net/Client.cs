using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using alink.Models;
using alink.Net.Data;

namespace alink.Net
{

    public class Client
    {
        private TcpClient _tcp;
        private BackgroundWorker _clientWorker;
        private IPEndPoint _ipEndPoint;
        private bool _stopCalled;
        
        private readonly IList<UserInfo> _users;

        public event EventHandler UsersChanged;
        public event EventHandler<ObjectEventArgs> IncomingData;
        public event EventHandler Connected;
        public event EventHandler<StringEventArgs> Disconnected;
        public event EventHandler<StringEventArgs> OnError;


        public string Nickname { get; private set; }
        public Guid ClientGuid { get; private set; }

        public Client()
        {
            _users = new List<UserInfo>();
        }

        #region Public properties

        public IList<UserInfo> Users { get { return _users; } }

        public bool IsConnected { get { return _tcp != null && _tcp.Connected && !_stopCalled; } }
        #endregion

        #region Public methods
        public bool Connect(IPAddress ip, int port, string nickname)
        {
            Nickname = nickname;
            return Connect(new IPEndPoint(ip, port));
        }

        public bool Connect(IPEndPoint ipEndpoint)
        {
            if (_tcp != null && _tcp.Connected)
            {
                throw new Exception("Already connected");
            }
            _ipEndPoint = ipEndpoint;
            _stopCalled = false;

            _tcp = new TcpClient(ipEndpoint.AddressFamily);
            _tcp.Connect(_ipEndPoint);

            if (_tcp.Connected)
            {
                _clientWorker = new BackgroundWorker();
                _clientWorker.DoWork += (o, e) => run();
                _clientWorker.RunWorkerCompleted += (o, e) => onComplete();
                _clientWorker.RunWorkerAsync();
                onConnected();
            }
            else
            {
                _stopCalled = true;
            }
            return _tcp.Connected;
        }

        public void Disconnect()
        {
            _stopCalled = true;
        }
        
        public void SendPacketToServer(Packet p)
        {
            if (_stopCalled)
                return;

            var stream = _tcp.GetStream();
            stream.WriteAsync(p.Bytes, 0, p.TotalSize);
        }
        #endregion

        #region Event helpers
        private void onError(string message)
        {
            OnError?.Invoke(this, new StringEventArgs(message));
        }

        private void onConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private void onDisconnected(string message)
        {
            Disconnected?.Invoke(this, new StringEventArgs(message));
        }

        private void onIncomingData(object o)
        {
            IncomingData?.Invoke(this, new ObjectEventArgs(o));
        }

        private void onUsersChanged()
        {
            UsersChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Private methods
        private void run()
        {
            try
            {
                var stream = _tcp.GetStream();
                var size = _tcp.ReceiveBufferSize;
                while (!_stopCalled)
                {
                    byte[] buffer = new byte[size];
                    stream.Read(buffer, 0, size);
                    handleIncomingPacket(new Packet(buffer));
                }
            }
            catch (Exception e)
            {
                _stopCalled = true;
                onError(e.Message);
            }
        }

        private void handleIncomingPacket(Packet packet)
        {
            int newOffset;
            
            if (packet.PacketType == NetConstants.PacketTypes.ServerRegister)
            {
                if (packet.DataSize == 0)
                    return;
                var text = PacketHelper.GetStringFromBytes(packet.DataBytes, 0, out newOffset);
                if (text == NetConstants.ServerRegisterString)
                {
                    var guidBytes = new byte[16];
                    Array.Copy(packet.DataBytes, newOffset, guidBytes, 0, 16);
                    var guid = new Guid(guidBytes);
                    ClientGuid = guid;
                    var registerPacket = PacketHelper.CreateUserRegistrationPacket(Nickname);
                    SendPacketToServer(registerPacket);
                }
                else
                {
                    onDisconnected("Invalid server response");
                    Disconnect();
                }
            }
            if (packet.PacketType == NetConstants.PacketTypes.UserJoined)
            {
                newOffset = 0;
                addClient(userInfoFromBytes(packet.DataBytes, newOffset, out newOffset));
            }
            if (packet.PacketType == NetConstants.PacketTypes.UserLeft)
            {
                newOffset = 0;
                var incomingUser = userInfoFromBytes(packet.DataBytes, newOffset, out newOffset);
                var user = getUserInfoFromGuid(incomingUser.Guid);
                removeClient(user);
            }
            if (packet.PacketType == NetConstants.PacketTypes.UserAlreadyHere)
            {
                var numClients = BitConverter.ToInt32(packet.DataBytes, 0);
                newOffset = 4;
                for (int i = 0; i < numClients; ++i)
                {
                    addClient(userInfoFromBytes(packet.DataBytes, newOffset, out newOffset));
                }
            }
            if (packet.PacketType == NetConstants.PacketTypes.ServerShutdown)
            {
                onDisconnected("Server shut down");
                Disconnect();
            }
            if (packet.PacketType == NetConstants.PacketTypes.MemoryData)
            {
                var guid = guidFromBytes(packet.DataBytes, 0, out newOffset);
                var user = getUserInfoFromGuid(guid);
                //Don't accept memory changed packets from ourselves
                if (user != null && user.Guid != ClientGuid && user.Approved)
                {
                    var memorySize = packet.DataSize - newOffset;
                    var memoryBytes = new byte[memorySize];
                    Array.Copy(packet.DataBytes, newOffset, memoryBytes, 0, memorySize);
                    var memoryData = ChangedDataFactory.BuildChangedData(memoryBytes);
                    onIncomingData(new IncomingMemoryData(user, memoryData));
                }
            }
            if (packet.PacketType == NetConstants.PacketTypes.ServerToUserChat)
            {
                var incomingUser = userInfoFromBytes(packet.DataBytes, 0, out newOffset);
                //Use the local user if available
                var user = getUserInfoFromGuid(incomingUser.Guid) ?? incomingUser;
                var text = PacketHelper.GetStringFromBytes(packet.DataBytes, newOffset, out newOffset);
                onIncomingData(new ChatData(user, text));
            }
        }

        private void addClient(UserInfo info)
        {
            lock (_users)
            {
                //Automatically approve self
                if (info.Guid == ClientGuid)
                    info.Approved = true;
                _users.Add(info);
                onIncomingData(new UserJoinedLeftData(info, true));
                onUsersChanged();
            }
        }

        private void removeClient(UserInfo info)
        {
            lock (_users)
            {
                _users.Remove(info);
                onIncomingData(new UserJoinedLeftData(info, false));
                onUsersChanged();
            }
        }

        private UserInfo getUserInfoFromGuid(Guid guid)
        {
            lock (_users)
            {
                return _users.FirstOrDefault(c => c.Guid == guid);
            }
        }

        private Guid guidFromBytes(byte[] buffer, int offset, out int newOffset)
        {
            newOffset = offset;
            var guidBytes = new byte[16];
            Array.Copy(buffer, newOffset, guidBytes, 0, 16);
            newOffset += 16;
            return new Guid(guidBytes);
        }

        private UserInfo userInfoFromBytes(byte[] buffer, int offset, out int newOffset)
        {
            newOffset = offset;
            var guid = guidFromBytes(buffer, newOffset, out newOffset);
            var name = PacketHelper.GetStringFromBytes(buffer, newOffset, out newOffset);
            return new UserInfo(name, guid);
        }

        private void onComplete()
        {
            _tcp = null;
            _clientWorker = null;
            _ipEndPoint = null;
            _users.Clear();
        }
        #endregion

    }

}
