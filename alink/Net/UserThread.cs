using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;

namespace alink.Net
{
    public class UserThread : NetSerializable
    {
        private BackgroundWorker _thread;
        private TcpClient _tcpClient;
        private CancellationTokenSource _cancelSource;

        private bool _stopCalled;

        public Guid UserGuid { get; private set; }
        public string Username { get; set; }
        public TcpClient TcpClient { get { return _tcpClient; } }
        public bool StopCalled { get { return _stopCalled; } }
        public bool IsRegistered { get; set; }
        //Currently unused
        public CancellationToken CancelToken { get { return _cancelSource.Token; } }

        public UserThread(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _thread = null;
            UserGuid = Guid.NewGuid();
        }

        public void Start(Action<UserThread> runDelegate, Action<UserThread> stoppedDelegate)
        {
            if (_thread != null)
                return;
            _stopCalled = false;
            _cancelSource = new CancellationTokenSource();
            _thread = new BackgroundWorker();
            _thread.DoWork += (o, e) => runDelegate(this);
            _thread.RunWorkerCompleted += (o, e) => stoppedDelegate?.Invoke(this);
            _thread.RunWorkerAsync();
        }

        public void Stop()
        {
            if (_thread == null)
                return;
            _stopCalled = true;
            _cancelSource.Cancel();
        }

        public byte[] GetBytes()
        {
            return PacketHelper.ConcatBytes(UserGuid.ToByteArray(), PacketHelper.GetStringBytes(Username));
        }
    }

}
