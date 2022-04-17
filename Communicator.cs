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
    public delegate void OnReceived(string message);
    public class Communicator
    {
        public event OnReceived OnReceived;
        public readonly List<Socket> sockets;
        private readonly bool mode;
        private static byte[] buffer = new byte[2048];
        public Communicator(string ip, bool mode)
        {
            sockets = new List<Socket>();
            this.mode = mode;

            string[] addresses = ip.Split(':');
            IPAddress address = IPAddress.Parse(addresses[0]);
            IPEndPoint point = new IPEndPoint(address, int.Parse(addresses[1]));
            Socket mainSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (mode)
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
                            Send(socket.RemoteEndPoint.ToString() + " joined the chat!", "System");
                            OnReceived?.Invoke("System:/:" + socket.RemoteEndPoint.ToString() + " joined the chat!");
                            while (socket.Connected)
                            {
                                if (socket.Available > 0)
                                {
                                    var receive = socket.Receive(buffer);
                                    if (receive > 0)
                                    {
                                        string msg = Encoding.UTF8.GetString(buffer);
                                        OnReceived?.Invoke(msg);
                                        Send(msg, smsg: true, s: socket);
                                    }
                                }
                                Thread.Sleep(300);
                            }
                            sockets.Remove(socket);
                            try
                            {
                                Send(socket.RemoteEndPoint.ToString() + " left from the chat!", "System");
                                OnReceived?.Invoke("System:/:" + socket.RemoteEndPoint.ToString() + " left from the chat!");
                            }
                            catch (Exception)
                            {

                            }
                        });
                        Thread.Sleep(2000);
                    }
                }).Start();
            }
            else
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
                                    OnReceived?.Invoke(Encoding.UTF8.GetString(buffer.Take(receive).ToArray()));
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
            }
        }

        public void Send(string message, string user = "", bool smsg = false, Socket s = null)
        {
            foreach (var x in s == null ? sockets : sockets.Where(x => x != s))
            {
                bool success = false;
                while (!success)
                {
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(smsg ? message : ((user == "" ? MainWindow.IP : user) + ":/:" + message));

                        x.SendBufferSize = bytes.Length;
                        x.Send(bytes);
                        success = true;
                    }
                    catch (Exception)
                    {
                        success = false;
                    }
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
