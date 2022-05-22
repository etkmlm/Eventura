using CoreliumCryptionEngine;
using CoreliumSocketAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eventura
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string IP;
        public static ISocketService service;
        private const int BUFFER_SIZE = 4096;
        private List<CoreliumSocket> allowed;
        
        public MainWindow()
        {
            InitializeComponent();

            allowed = new List<CoreliumSocket>();

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("To use this app, you need an internet connection!");
                Environment.Exit(0);
            }

            MouseLeftButtonDown += (a, b) => DragMove();
            btnClose.Click += (a, b) =>
            {
                service?.Stop();
                Environment.Exit(0);
            };
            IP = new System.Net.WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

            btnConnect.Click += (a, b) =>
            {
                string[] info = txtIP.Text.Split(':');
                if (!int.TryParse(info[1], out int port))
                {
                    MessageBox.Show("Please enter a valid ip!");
                    return;
                }
                service?.Stop();

                service = new TCPClient(IP, BUFFER_SIZE, info[0], port).Start();
                ((TCPClient)service).Connected += () =>
                {
                    Add("System", "Connected!");
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => service.Send("pass" + txtPass.Text));
                };
                var server = service.GetServer().SetTimeout(10);
                allowed.Add(server);

                server.Received += OnClientReceived;
                server.Disconnected += () => Add("System", "Connection lost.");
            };

            btnSend.Click += (a, b) => Send("msg", txtMessage.Text);
            txtMessage.KeyDown += (a, b) =>
            {
                if (b.Key == Key.Enter)
                    Send("msg", txtMessage.Text);
            };

            btnStart.Click += (a, b) =>
            {
                string[] info = txtIP.Text.Split(':');
                if (!int.TryParse(info[1], out int port))
                {
                    MessageBox.Show("Please enter a valid ip!");
                    return;
                }
                service?.Stop();

                service = new TCPServer(BUFFER_SIZE, info[0], port).SetTimeout(10).Start();
                var server = (TCPServer)service;
                Add("System", "Server started!");

                server.NameReceived += (socket, name) => Add("System", name + " joined, waiting for password...");
                server.Received += OnServerReceived;
            };
        }

        private void OnClientReceived(byte[] buffer, int received)
        {
            string message = Encoding.UTF8.GetString(buffer).Substring(0, received);

            if (message == "kicked")
                Add("Server", "Wrong password!");
            else if (message.StartsWith("msg"))
            {
                string[] data = message.Substring(3).Split(new char[] { ',' }, 2);
                if (data.Length == 1)
                    Add("System", data[0]);
                else
                    Add(data[0], data[1]);
            }
        }

        private void OnServerReceived(CoreliumSocket socket, byte[] buffer) => Dispatcher.Invoke(() =>
        {
            string message = Encoding.UTF8.GetString(buffer);

            if (message.StartsWith("pass"))
            {
                if (message.Substring(4) == txtPass.Text)
                {
                    allowed.Add(socket);
                    Send("msg", socket.Name + " joined to chat!", from: "System");
                }
                else
                {
                    socket.Send("kicked");
                    Send("msg", socket.Name + " kicked due to entered wrong password!", from: "System");
                    socket.Close();
                }
            }
            else if (message.StartsWith("msg"))
                Send("msg", message.Substring(3), allowed.Where(x => x != socket), socket.Name);
        });

        private void Send(string prefix, string msg, IEnumerable<CoreliumSocket> to = null, string from = null) => Dispatcher.Invoke(() =>
        {
            (to?.ToList() ?? allowed).ForEach(x => x.Send(prefix + (service is TCPServer ? IP + "," : "") + msg));
            Add(from ?? IP, msg.Replace(from == null ? IP + "," : from + ",", ""));
            txtMessage.Clear();
        });

        private void Add(string user, string msg) =>
            Dispatcher.Invoke(() =>
            {
                Run run = new Run(msg);
                Bold bold = new Bold(new Run(user + ": "));
                Paragraph p = new Paragraph();
                p.Inlines.Add(bold);
                p.Inlines.Add(run);
                txtMessages.Document.Blocks.Add(p);
                txtMessages.ScrollToEnd();
            });
    }
}
