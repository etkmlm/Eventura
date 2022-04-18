using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Eventura
{
    public delegate void OnReceived(string message, Socket socket = null);
    public class Communicator
    {
        public event OnReceived OnReceived;
        public readonly List<Socket> sockets;
        public readonly bool mode;
        public readonly string pass;
        private static byte[] buffer = new byte[2048];
        public readonly List<Socket> allowed;
        public Communicator(string ip, bool mode, string pass)
        {
            sockets = new List<Socket>();
            this.mode = mode;
            this.pass = pass;

            string[] addresses = ip.Split(':');
            IPAddress address = IPAddress.Parse(addresses[0]);
            IPEndPoint point = new IPEndPoint(address, int.Parse(addresses[1]));
            allowed = new List<Socket>();
            Socket mainSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (mode)
                StartServer(mainSocket, point);
            else
                StartClient(mainSocket, point);
        }

        private void StartClient(Socket mainSocket, IPEndPoint point)
        {
            try
            {
                mainSocket.Connect(point);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
            new Thread(() =>
            {
                sockets.Add(mainSocket);
                while (mainSocket.Connected)
                {
                    if (mainSocket.Available > 0)
                    {
                        try
                        {
                            var receive = mainSocket.Receive(buffer);
                            if (receive > 0)
                                OnReceived?.Invoke(Encoding.UTF8.GetString(buffer.Take(receive).ToArray()), mainSocket);
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(300);
                            continue;
                        }
                    }
                    Thread.Sleep(300);
                }
                sockets.Remove(mainSocket);
            }).Start();
            SendTo("|SYS|pass" + pass, mainSocket, smsg: true);
        }

        private void StartServer(Socket mainSocket, IPEndPoint point)
        {
            mainSocket.Bind(point);
            mainSocket.Listen(10);
            new Thread(() =>
            {
                OnReceived?.Invoke("System:/:Server is started.");
                while (true)
                {
                    var socket = mainSocket.Accept();
                    ThreadPool.QueueUserWorkItem((a) =>
                    {
                        sockets.Add(socket);
                        OnReceived?.Invoke("System:/:" + socket.RemoteEndPoint.ToString() + " joined the chat!");
                        while (socket.Connected)
                        {
                            if (socket.Available > 0)
                            {
                                var receive = socket.Receive(buffer);
                                if (receive > 0)
                                {
                                    string msg = Encoding.UTF8.GetString(buffer);
                                    OnReceived?.Invoke(Encoding.UTF8.GetString(buffer.Take(receive).ToArray()), socket);
                                    if (allowed.Contains(socket))
                                        Send(msg, smsg: true, s: socket);
                                }
                            }
                            Thread.Sleep(300);
                        }
                        sockets.Remove(socket);
                        Send(socket.RemoteEndPoint.ToString() + " left from the chat!", "System");
                        OnReceived?.Invoke("System:/:" + socket.RemoteEndPoint.ToString() + " left from the chat!");
                    });
                    Thread.Sleep(2000);
                }
            }).Start();
        }

        public void Send(string message, string user = "", bool smsg = false, Socket s = null)
        {
            if (mode)
            {
                foreach (var x in s == null ? allowed : allowed.Where(x => x != s))
                    SendTo(message, x, user, smsg);
            }
            else
            {
                foreach (var x in s == null ? sockets : sockets.Where(x => x != s))
                    SendTo(message, x, user, smsg);
            }
        }

        public void SendTo(string message, Socket s, string user = "", bool smsg = false)
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(smsg ? message : ((user == "" ? MainWindow.IP : user) + ":/:" + message));

                    s.SendBufferSize = bytes.Length;
                    s.Send(bytes);
                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
            }
        }

        public void Close()
        {
            foreach (var x in sockets)
                x.Close();
        }
    }
}
