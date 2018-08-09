using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace ConsoleApp4
{
    public static class Logger
    {
        public static void Debug(string message)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }

    public class Server
    {
        private ManualResetEvent AllDone = new ManualResetEvent(false);

        public IPEndPoint IPEndPoint { get; private set; }

        private Socket ClientSocket { get; set; }
        private byte[] ClientBuffer { get; set; } = new byte[100000];

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public List<string> ReceiveDatas { get; } = new List<string>();

        public void Service()
        {
            lock (ReceiveDatas)
            {
                foreach (var data in ReceiveDatas)
                {
                    OnReceive(data);
                }
                ReceiveDatas.Clear();
            }
        }

        public void OnReceive(string message)
        {
            Logger.Debug(message);

            if (message == "hoge")
            {
                Send("test");
            }
        }

        private void _Close()
        {
            ClientSocket.Close();
            ClientSocket = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
            Logger.Debug("ソケットを閉じました。");
        }

        public void Open(int port)
        {
            using (var listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                this.IPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listenerSocket.Bind(this.IPEndPoint);

                listenerSocket.Listen(1);
                Logger.Debug($"サーバーを起動しました ... [{listenerSocket.LocalEndPoint}]");

                while (true)
                {
                    AllDone.Reset();
                    listenerSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenerSocket);
                    AllDone.WaitOne();
                }
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            AllDone.Set();

            var listenerSocket = asyncResult.AsyncState as Socket;
            var clientSocket = listenerSocket.EndAccept(asyncResult);
            
            if(ClientSocket != null)
            {
                Logger.Debug($"接続数が上限なため接続をキャンセルします。: {clientSocket.RemoteEndPoint}");
                clientSocket.Close();
                return;
            }

            ClientSocket = clientSocket;
            Logger.Debug($"接続: {clientSocket.RemoteEndPoint}");
            Connected?.Invoke(this, EventArgs.Empty);
            
            try
            {
                clientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch(Exception)
            {
                _Close();
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            try
            {
                int bytes = ClientSocket.EndReceive(asyncResult);
                if (bytes > 0)
                {
                    lock (ReceiveDatas)
                    {
                        ReceiveDatas.Add(Encoding.UTF8.GetString(ClientBuffer, 0, bytes));
                    }
                    ClientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    _Close();
                }
            }
            catch(Exception)
            {
                _Close();
            }
        }

        private void Send(string data)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                ClientSocket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
            }
            catch(Exception)
            {
                _Close();
                Console.WriteLine("送れませんでした。Send");
            }
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                var byteSize = ClientSocket.EndSend(asyncResult);
            }
            catch (Exception)
            {
                _Close();
                Console.WriteLine("送れませんでした。SendCallback");
            }
        }
    }
}
