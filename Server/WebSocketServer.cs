using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1.Server
{

    public class WebSocketServer
    {
        public readonly int Port;
        private readonly TcpListener _tcpListener;
        private readonly Task _listenTask;
        private List<TcpClient> clients =new List<TcpClient>();
        public Task ListenTask
        {
            get
            {
                return _listenTask;
            }
        }

        public WebSocketServer(string address,int port)
        {
            Port = port;
            _tcpListener = new TcpListener(IPAddress.Parse(address), Port);
            _tcpListener.Start();

            _listenTask = Task.Factory.StartNew(() => ListenLoop());

        }

        private async void ListenLoop()
        {
            while (true)
            {

                TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                if (client == null)
                    break;
                var webSocketclient = new WebSocketClient(client);
                clients.Add(client);
                Task.Factory.StartNew(webSocketclient.Listen);
            }
        }
    }

}
