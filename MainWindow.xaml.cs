using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
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
        public static Communicator communicator;
        public MainWindow()
        {
            InitializeComponent();
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("To use this app, you need an internet connection!");
                Environment.Exit(0);
            }

            MouseLeftButtonDown += (a, b) => DragMove();
            btnClose.Click += (a, b) =>
            {
                communicator?.Close();
                Environment.Exit(0);
            };
            IP = new System.Net.WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();

            btnConnect.Click += (a, b) =>
            {
                communicator = new Communicator(txtIP.Text, false);
                communicator.OnReceived += OnReceived;
            };

            btnSend.Click += (a, b) => Send(txtMessage.Text);
            txtMessage.KeyDown += (a, b) =>
            {
                if (b.Key == Key.Enter)
                    Send(txtMessage.Text);
            };

            btnStart.Click += (a, b) =>
            {
                communicator = new Communicator(txtIP.Text, true);
                communicator.OnReceived += OnReceived;
            };
        }

        private void OnReceived(string message)
        {
            string[] content = message.Split(new string[] { ":/:" }, StringSplitOptions.None);
            if (content.Length == 1)
                Add("", content[0]);
            else
                Add(content[0], content[1]);
        }

        private void Send(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            communicator?.Send(text);
            Add(IP, text);
            txtMessage.Clear();
        }

        private void Add(string user, string msg)
        {
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
}
