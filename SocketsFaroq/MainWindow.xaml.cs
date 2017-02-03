using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Windows.Threading;
using ComLibb;

namespace SocketsFaroq
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool running = true;
        static bool areTasksStarted = false;

        public MainWindow()
        {
            InitializeComponent();

            tbPort.Text = @"23000";

            List<string> strIPAdresses = SocketServerEx.GetAllIpAddresses();

            foreach (string str in strIPAdresses)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = str;
                cbIPAddress.Items.Add(itm);
            }

            //var localIPs = Dns.GetHostEntry(Dns.GetHostName());
            //tbIpAddress.Text = localIPs.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
            //tbPort.Text = "23";
            //btnStartServer.Click += BtnStartServer_Click;
            //btnClose.Click += BtnClose_Click;
            //btnSend.Click += BtnSend_Click;
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (tbMessage.Text != string.Empty)
            {
                var msg = tbMessage.Text;
                SocketServerEx.Send(msg);
                tbMessage.Text = string.Empty;
                tbConsoleOutput.Text = tbConsoleOutput.Text + DateTime.Now.ToString("h:mm:ss tt") + " Server:\n" + msg + "\n";
            }

        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            SocketServerEx.CloseSocket();
            btnSend.IsEnabled = false;
            //running = false;
            btnStartServer.IsEnabled = true;
            lblStatus.Content = "Closed socket";
        }

        private void BtnStartServer_Click(object sender, RoutedEventArgs e)
        {
            var strPort = tbPort.Text;
            //var strIp = tbIpAddress.Text;
            ListBoxItem selectedItem = (ListBoxItem)cbIPAddress.SelectedItem;
            string strIpAddress = (string)selectedItem.Content;
            if (!areTasksStarted)
            {
                Task.Run(() => SocketServerEx.StartListening(strPort, strIpAddress));
                new Task(GetDataAndUpdateConsoleOutputBox).Start();
                Task.Run(() => UpdatedClientListBox());
                areTasksStarted = true;
            }
            lblStatus.Content = "listening";
            btnStartServer.IsEnabled = false;
            btnSend.IsEnabled = true;

            //Task.Run(() => SocketServerEx.StartListening(strPort, strIp));
            //lblStatus.Content = "listening";
            //btnStartServer.IsEnabled = false;
            //var t = new Task(GetDataAndUpdateConsoleOutputBox);
            //t.Start();
        }

        private void UpdatedClientListBox()
        {
            while (running)
            {
                SocketServerEx.GetClientConnectedDisconnectedEvent().WaitOne();
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate
                    {
                        while (lbClient.Items.Count > 0)
                        {
                            lbClient.Items.RemoveAt(0);
                        }
                        foreach (Socket client in SocketServerEx.GetSockets())
                        {
                            string str = client.RemoteEndPoint.ToString();
                            ListBoxItem item = new ListBoxItem();
                            item.Content = str;
                            lbClient.Items.Add(item);
                        }
                    }));
            }
        }

        private void GetDataAndUpdateConsoleOutputBox()
        {

            while (running)
            {
                SocketServerEx.GetReceivedDataEvent().WaitOne();
                //SocketServerEx.GetAutoResetEvent().WaitOne();
                var str = SocketServerEx.GetData();
                var size = str.Length;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(
                        delegate
                        {
                            tbConsoleOutput.Text = tbConsoleOutput.Text + DateTime.Now.ToString("h:mm:ss tt") + " Client:\n" + str;
                            tbConsoleOutput.ScrollToEnd();
                        }));
            }
        }

        private void cbIPAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void lbClient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}