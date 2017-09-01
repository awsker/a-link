using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using alink.Models;
using alink.Net;
using alink.Net.Data;
using alink.Utils;

namespace alink.UI
{
    public partial class LobbyContainer : UserControl
    {
        private Server _server;
        private Client _client;

        private NetSettings _lastNetSettings;

        private object _processManagerLock = new object();
        private ProcessManager _processManager;
        
        public LobbyContainer()
        {
            InitializeComponent();
            NetSettings = new NetSettings(string.Empty, 44544, 44544, string.Empty);
        }

        #region Public Properties
        public string Nickname
        {
            get { return nicknameTextBox.Text; }
            set { nicknameTextBox.Text = value; }
        }

        public NetSettings NetSettings
        {
            get { return _lastNetSettings; }
            set
            {
                _lastNetSettings = value;
                Nickname = _lastNetSettings.Nickname;
            }
        }

        public ProcessManager ProcessManager
        {
            get { return _processManager; }
            set
            {
                if (_processManager != null)
                {
                    _processManager.LogOutput -= onProcessManagerLogMessage;
                    _processManager.MemoryChanged -= onProcessManagerMemoryChanged;
                }
                lock (_processManagerLock)
                {
                    _processManager = value;
                }
                if (_processManager != null)
                {
                    _processManager.LogOutput += onProcessManagerLogMessage;
                    _processManager.MemoryChanged += onProcessManagerMemoryChanged;
                }
            }
        }
        #endregion

        #region UI Events

        private void hostButton_Click(object sender, EventArgs e)
        {
            if (!validateNicknameSet())
                return;

            if (_server == null)
            {
                var dialog = new HostInfoForm();
                dialog.Port = _lastNetSettings.HostPort;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _lastNetSettings.HostPort = dialog.Port;
                    _lastNetSettings.Nickname = Nickname;

                    host(dialog.Port);
                    join(_server.IPAddresses[0], _server.Port, Nickname);

                    statusTextSuccess("Hosting on port " + _server.Port);
                    printToChat("Server started", Color.Green);
                }
            }
            else
            {
                disconnect();
                stopHost();
                statusTextSuccess("Server stopped");
                printToChat("Server stopped", Color.Green);
            }
        }

        private void joinButton_Click(object sender, EventArgs e)
        {
            if (!validateNicknameSet())
                return;

            if (_client != null)
            {
                disconnect();
            }
            else
            {
                var dialog = new JoinInfoForm();
                dialog.Address = _lastNetSettings.Address;
                dialog.Port = _lastNetSettings.JoinPort;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _lastNetSettings.Address = dialog.Address;
                    _lastNetSettings.JoinPort = dialog.Port;
                    _lastNetSettings.Nickname = Nickname;

                    Exception lastEx = null;
                    IPAddress address;
                    if (IPAddress.TryParse(dialog.Address, out address))
                    {
                        try
                        {
                            join(address, dialog.Port, Nickname);
                        }
                        catch (Exception exception)
                        {
                            lastEx = exception;
                        }
                    }
                    else
                    {

                        IPAddress[] addresses = new IPAddress[0];
                        try
                        {
                            addresses = Dns.GetHostAddresses(dialog.Address);
                        }
                        catch (Exception exception)
                        {
                            lastEx = exception;
                        }
                        foreach (var dnsResolve in addresses)
                        {
                            try
                            {
                                join(dnsResolve, dialog.Port, Nickname);
                                lastEx = null;
                                break;
                            }
                            catch (Exception ex)
                            {
                                lastEx = ex;
                            }
                        }
                    }
                    if (lastEx != null && (_client == null || !_client.IsConnected))
                    {
                        MessageBox.Show("Error finding host address: " + lastEx.Message, "Join Error");
                    }
                    if (_client != null && !_client.IsConnected)
                        disconnect();
                }
            }
        }

        private void chatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_client != null && _client.IsConnected && e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(currentChatText))
            {
                _client.SendPacketToServer(PacketHelper.CreateUserToServerChatMessage(currentChatText));
                chatTextBox.Text = string.Empty;
            }
        }
        #endregion
        
        #region Private methods   
        private void host(int port)
        {
            if (_server != null)
                return;
            _server = new Server(port);
            addServerEvents();
            _server.Host();
            hostButton.Text = "Stop";
            joinButton.Enabled = false;
        }

        private void join(IPAddress ipaddress, int port, string nickname)
        {
            
            if (_client != null && _client.IsConnected)
                disconnect();

            if (_server == null)
            {
                joinButton.Text = "Disc.";
                hostButton.Enabled = false;
                
            }
            clearChat();
            _client = new Client();
            addClientEvents();
            try
            {
                _client.Connect(ipaddress, port, nickname);
            }
            catch (Exception)
            {
                removeClientEvents();
                throw;
            }
            if (_client.IsConnected && (_processManager == null || !_processManager.Running))
            {
                printToChat("Remember to select and 'Attach' a process", Color.DarkRed);
            }
            chatTextBox.Enabled = true;
            richTextBox1.Enabled = true;
            logTextBox.Enabled = true;
            nicknameTextBox.Enabled = false;
        }

       private void stopHost()
        {
            if (_server == null)
                return;

            _server.Stop();
            removeServerEvents();
            _server = null;
            hostButton.Text = "Host";
            joinButton.Enabled = true;
        }

        private void disconnect()
        {
            if (_client == null)
                return;

            _client.Disconnect();
            removeClientEvents();
            _client = null;
            joinButton.Text = "Join";
            hostButton.Enabled = true;
            nicknameTextBox.Enabled = true;
            chatTextBox.Enabled = false;
            richTextBox1.Enabled = false;
            logTextBox.Enabled = false;
            clientListBox.DataSource = null;
        }


        private void addServerEvents()
        {
            if (_server == null)
                return;

            _server.OnError += onServerError;
        }

        private void removeServerEvents()
        {
            if (_server == null)
                return;

            _server.OnError -= onServerError;
        }

        private void addClientEvents()
        {
            if (_client == null)
                return;

            _client.UsersChanged += onUsersChanged;
            _client.OnError += onClientError;
            _client.Connected += onClientConnected;
            _client.Disconnected += onClientDisconnected;
            _client.IncomingData += onIncomingData;
        }
        
        private void removeClientEvents()
        {
            if (_client == null)
                return;

            _client.UsersChanged -= onUsersChanged;
            _client.OnError -= onClientError;
            _client.Connected -= onClientConnected;
            _client.Disconnected -= onClientDisconnected;
            _client.IncomingData -= onIncomingData;
        }

        private bool validateNicknameSet()
        {
            if (string.IsNullOrWhiteSpace(Nickname))
            {
                errorProvider1.SetError(nicknameTextBox, "Username must be set");
                return false;
            }
            errorProvider1.SetError(nicknameTextBox, null);
            return true;
        }

        private void handleIncomingData(object o)
        {
            if (o is IncomingMemoryData)
            {
                var id = (IncomingMemoryData) o;
                if (_processManager != null)
                {
                    id.ChangedData.PushIntoProcessManager(_processManager, id.User.Username);
                }
            }
            if (o is ChatData)
            {
                var cd = (ChatData) o;
                string formattedText = $"<{cd.User.Username}> {cd.Message}";
                printToChat(formattedText, Color.Black);
            }
            if (o is UserJoinedLeftData)
            {
                var jld = (UserJoinedLeftData) o;
                string formattedText;
                if (jld.Joined)
                    formattedText = $"{jld.User.Username} joined the lobby";
                else
                    formattedText = $"{jld.User.Username} left the lobby";
                printToChat(formattedText, Color.Green);
            }
        }

        private void statusTextSuccess(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { statusTextSuccess(text); }));
                return;
            }
            statusLabel.Text = text;
            statusLabel.ForeColor = Color.Green;
        }

        private void statusTextError(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { statusTextError(text); }));
                return;
            }
            statusLabel.Text = text;
            statusLabel.ForeColor = Color.DarkRed;
        }

        private void statusTextClear()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { statusTextClear(); }));
                return;
            }
            statusLabel.Text = string.Empty;
            statusLabel.ForeColor = Color.Black;
        }

        private void printToChat(string text, Color color)
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;

            richTextBox1.SelectionColor = color;
            richTextBox1.AppendText(text + "\r\n");
            richTextBox1.SelectionColor = richTextBox1.ForeColor;

            richTextBox1.ScrollToCaret();
        }

        private void clearChat()
        {
            richTextBox1.Clear();
        }

        private string currentChatText
        {
            get { return chatTextBox.Text; }
        }
        #endregion

        #region Events
        private void onProcessManagerMemoryChanged(object sender, MemoryChangedEventArgs memoryChangedEventArgs)
        {
            if (_client != null && _client.IsConnected)
            {
                var packet = new Packet(NetConstants.PacketTypes.MemoryData, memoryChangedEventArgs.ChangedData.GetNetworkBytes());
                _client.SendPacketToServer(packet);
            }
        }

        private void onUsersChanged(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { onUsersChanged(sender, eventArgs); }));
                return;
            }
            clientListBox.DataSource = null;
            clientListBox.DisplayMember = "Username";
            clientListBox.DataSource = _client.Users;
        }
        
        private void onServerError(object sender, StringEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { onServerError(sender, e); }));
                return;
            }
            statusTextError("Server Error: " + e.Message);
            stopHost();
        }

        private void onClientError(object sender, StringEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { onClientError(sender, e); }));
                return;
            }
            statusTextError("Client Error: " + e.Message);
            disconnect();
            statusTextError("Lost connection to server");
        }

        private void onIncomingData(object sender, ObjectEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { onIncomingData(sender, e); }));
                return;
            }
            handleIncomingData(e.Object);
        }


        private void onClientConnected(object sender, EventArgs e)
        {
            //Only show this message when we're not currently the host
            if (_server != null)
                statusTextSuccess($"Connected to {_lastNetSettings.Address}:{_lastNetSettings.JoinPort}");
        }

        private void onClientDisconnected(object sender, StringEventArgs e)
        {
            disconnect();
            statusTextError("Disconnected: " + e.Message);
        }
        
        private void onProcessManagerLogMessage(object sender, LogMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { onProcessManagerLogMessage(sender, e); }));
                return;
            }
            printToChat(e.Message, Color.CadetBlue);
        }
        #endregion        

    }
}
