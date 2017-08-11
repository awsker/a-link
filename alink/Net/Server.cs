using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using alink.Models;

namespace alink.Net
{
    public class Server
    {
        private IPAddress[] _ipAddress;
        private int _port;
        private bool _hosting;
        private bool _stopCalled;

        private ConcurrentBag<TcpListener> _tcpListeners;
        private ConcurrentBag<UserThread> _clients;

        public event EventHandler<StringEventArgs> OnError;

        public Server(int port)
        {
            _port = port;
            _ipAddress = getIpAdresses();
        }

        #region Public properties
        public int Port { get { return _port; } }
        public IPAddress[] IPAddresses { get {  return _ipAddress;} }
        #endregion

        #region Public methods
        public void Host()
        {
            if (_hosting)
                return;
            
            _tcpListeners = new ConcurrentBag<TcpListener>();
            _clients = new ConcurrentBag<UserThread>();

            _stopCalled = false;
            foreach (var ip in getIpAdresses())
            {
                var localIp = ip;
                var sw = new BackgroundWorker();
                sw.DoWork += (o, e) => run(localIp);
                //sw.RunWorkerCompleted += (o, e) => onComplete();
                sw.RunWorkerAsync();
            }
            _hosting = true;
        }

        public void Stop()
        {
            if (!_hosting)
                return;
            var stopPacket = new Packet(NetConstants.PacketTypes.ServerShutdown, new byte[0]);
            sendPacketToAllClients(stopPacket);

            foreach (var tcp in _tcpListeners)
            {
                tcp.Stop();
            }
            foreach (var client in _clients)
            {
                client.TcpClient.Close();
                client.Stop();
            }
            _tcpListeners = null;
            _hosting = false;
            _stopCalled = true;
        }
        #endregion

        #region Private methods
        private IPAddress[] getIpAdresses()
        {
            return Dns.GetHostAddresses(Dns.GetHostName());
        }

        private void run(IPAddress ip)
        {
            try
            {
                var tcp = new TcpListener(ip, _port);
                tcp.Start();
                _tcpListeners.Add(tcp);
                while (!_stopCalled)
                {
                    acceptIncomingConnections(tcp);
                }
            }
            catch (Exception e)
            {
                _stopCalled = true;
                OnError?.Invoke(this, new StringEventArgs(e.Message));
            }
        }

        private void acceptIncomingConnections(TcpListener tcp)
        {
            while (!_stopCalled)
            {
                try
                {
                    var client = tcp.AcceptTcpClient();
                    var clientThread = new UserThread(client);
                    var registerPacket = PacketHelper.CreateServerRegistrationPacket(clientThread.UserGuid);
                    sendPacketToClient(registerPacket, clientThread);
                    _clients.Add(clientThread);
                    clientThread.Start(clientListenLoop, clientStopped);
                }
                catch (SocketException)
                {
                    break;
                }
            }
            sendShutdownToAll();
        }

        private void clientListenLoop(UserThread client)
        {
            var stream = client.TcpClient.GetStream();
            var size = client.TcpClient.ReceiveBufferSize;
            try
            {
                while (!client.StopCalled)
                {
                    byte[] buffer = new byte[size];
                    stream.Read(buffer, 0, size);
                    handleIncomingPacket(client, new Packet(buffer));
                }
            }
            catch (IOException)
            {
                //Stream was closed, most likely due to the server shutting down
                client.Stop();
            }
        }

        private void handleIncomingPacket(UserThread sender, Packet packet)
        {
            int newOffset;
            if (packet.PacketType == NetConstants.PacketTypes.UserRegister)
            {
                var registerString = PacketHelper.GetStringFromBytes(packet.DataBytes, 0, out newOffset);
                if (registerString == NetConstants.UserRegisterString && !sender.IsRegistered)
                {
                    var name = PacketHelper.GetStringFromBytes(packet.DataBytes, newOffset, out newOffset);
                    sender.Username = name;
                    sender.IsRegistered = true;

                    var joinPacket = PacketHelper.CreateUserJoinedPacket(sender);
                    sendPacketToAllClientsExcept(joinPacket, sender.UserGuid, true);

                    var clientsPacket = PacketHelper.CreateUsersAlreadyHerePacket(AllRegisteredClients);
                    //Send info of all clients already in lobby to new client
                    sendPacketToClient(clientsPacket, sender);
                }
            }
            if (packet.PacketType == NetConstants.PacketTypes.UserLeft)
            {
                var leftPacket = PacketHelper.CreateUserLeftPacket(sender);
                sendPacketToAllClientsExcept(leftPacket, sender.UserGuid, true);
            }

            if (packet.PacketType == NetConstants.PacketTypes.MemoryData && sender.IsRegistered)
            {
                //Add the clients Guid in front of the data
                var newData = PacketHelper.ConcatBytes(sender.UserGuid.ToByteArray(), packet.DataBytes);
                var newPacket = new Packet(NetConstants.PacketTypes.MemoryData, newData);
                sendPacketToAllClientsExcept(newPacket, sender.UserGuid, true);
            }
            
            if (packet.PacketType == NetConstants.PacketTypes.UserChat)
            {
                var text = PacketHelper.GetStringFromBytes(packet.DataBytes, 0, out newOffset);
                var chatPacket = PacketHelper.CreateServerToUserChatMessage(sender, text);
                sendPacketToAllClients(chatPacket, true);
            }
        }

        private void sendPacketToAllClients(Packet p, bool onlyRegistered = false)
        {
            var clientsToInclude = onlyRegistered ? _clients.Where(c => c.IsRegistered) : _clients;
            foreach (var client in clientsToInclude)
            {
                sendPacketToClient(p, client);
            }
        }

        private void sendPacketToAllClientsExcept(Packet p, Guid except, bool onlyRegistered = false)
        {
            var clientsToInclude = onlyRegistered ? _clients.Where(c => c.IsRegistered && c.UserGuid != except) : _clients.Where(c => c.UserGuid != except);
            foreach (var client in clientsToInclude)
            {
                
                sendPacketToClient(p, client);
            }
        }

        private void sendPacketToClient(Packet p, UserThread client)
        {
            if (client.StopCalled)
                return;
            try
            {
                var stream = client.TcpClient.GetStream();
                stream.Write(p.Bytes, 0, p.TotalSize);
            }
            catch
            {
                dropClient(client);
            }
        }

        private void sendShutdownToAll()
        {
            var packet = new Packet(NetConstants.PacketTypes.ServerShutdown, new byte[0]);
            sendPacketToAllClients(packet);
            foreach (var c in _clients)
                c.Stop();
        }

        private void dropClient(UserThread client)
        {
            client.Stop();
            var disconnectPacket = new Packet(NetConstants.PacketTypes.UserLeft, client.GetBytes());
            sendPacketToAllClients(disconnectPacket);
        }

        private void clientStopped(UserThread client)
        {
            client.TcpClient.Close();
        }

        private IList<UserThread> AllRegisteredClients
        {
            get { return _clients.Where(c => c.IsRegistered).ToList(); }
        }
        #endregion

    }
}
