using ComLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace TcpServerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool running = true;
        static bool areTasksStarted = false;

        TcpListnerEx tcpListener;

        public MainWindow()
        {
            InitializeComponent();
            tbPort.Text = @"80";

            tcpListener = new TcpListnerEx();

            List<string> strIPAdresses = TcpListnerEx.GetAllIpAddresses();
            foreach (string str in strIPAdresses)
            {
                ListBoxItem itm = new ListBoxItem();
                itm.Content = str;
                cbIPAddresses.Items.Add(itm);
            }
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string strPort = tbPort.Text;
                ListBoxItem selectedItem = (ListBoxItem)cbIPAddresses.SelectedItem;
                string strIPAddress = (string)selectedItem.Content;
                if (!areTasksStarted)
                {
                    Task.Run(() => tcpListener.StartServer(IPAddress.Parse(strIPAddress), int.Parse(strPort)));
                    new Task(GetDataAndUpdateConsoleOutputBox).Start();
                    Task.Run(() => UpdateClientsListBox());
                    areTasksStarted = true;
                }
                lbStatus.Content = "Listening";
                btnStartServer.IsEnabled = false;
                btnSend.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void GetDataAndUpdateConsoleOutputBox()
        {
            while (running)
            {
                tcpListener.ReceivedDataEvent.WaitOne();
                string str = tcpListener.GetData();
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate
                    {
                        tbConsoleOutPut.Text = DateTime.Now.ToString("h:mm:ss tt") +
                        " Client: " + str + tbConsoleOutPut.Text;
                    }));

            }
        }

        private void UpdateClientsListBox()
        {
            while (running)
            {
                tcpListener.ClientConnectedDisconnectedEvent.WaitOne();
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate
                    {
                        while (lbClients.Items.Count > 0)
                        {
                            lbClients.Items.RemoveAt(0);
                        }
                        foreach (TcpClient client in tcpListener.ClientList)
                        {
                            string str = client.Client.RemoteEndPoint.ToString();
                            ListBoxItem item = new ListBoxItem();
                            item.Content = str;
                            lbClients.Items.Add(item);
                        }
                    }));
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbPayLoad.Text)) return;
            try
            {
                tbConsoleOutPut.Text = DateTime.Now.ToString("h:mm:ss tt") + " Server: " + tbPayLoad.Text + "\n" + tbConsoleOutPut.Text;
                tcpListener.Send(Encoding.ASCII.GetBytes( tbPayLoad.Text));
                tbPayLoad.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {

            btnStartServer.IsEnabled = true;
            btnSend.IsEnabled = false;
            lbStatus.Content = "Closed socket";
        }
    }
}
