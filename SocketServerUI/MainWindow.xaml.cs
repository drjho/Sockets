using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ComLib;
using System.Windows.Threading;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace SocketServerUI
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
            foreach(string str in strIPAdresses)
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
                    Task.Run(() => SocketServerEx.StartListening(strPort, strIPAddress));
                    new Task(GetDataAndUpdateConsolOutputBox).Start();
                    Task.Run(() => UpdateClientsListBox());
                    areTasksStarted = true;
                }
                lbStatus.Content = "Listening";
                btnStartServer.IsEnabled = false;
                btnSend.IsEnabled = true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            SocketServerEx.CloseSocket();
            btnStartServer.IsEnabled = true;
            btnSend.IsEnabled = false;
            lbStatus.Content = "Closed socket";
        }
        
        //private void GetDataAndUpdateClients()
        //{
        //    while (running)
        //    {
        //        SocketServerEx.GetClientDisconnectedEvent().WaitOne();
        //        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
        //            new ThreadStart(delegate
        //            {
        //                foreach (Socket client in SocketServerEx.GetSockets())
        //                {
        //                    string str = client.RemoteEndPoint.ToString();
        //                    if (lbClients.Items.Cast<ListBoxItem>().Any(x => x.Content.Equals(str)))
        //                    {
        //                        ListBoxItem item = new ListBoxItem();
        //                        item.Content = str;
        //                        lbClients.Items.Remove(item);
        //                    }
        //                }
        //                SocketServerEx.GetRemovedClientFromListboxEvent().Set();
        //            }));
        //    }
        //}
        private void UpdateClientsListBox()
        {
            while (running)
            {
                SocketServerEx.GetClientConnectedDisconnectedEvent().WaitOne();
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                    new ThreadStart(delegate
                    {
                        while(lbClients.Items.Count > 0)
                        {
                            lbClients.Items.RemoveAt(0);
                        }
                        foreach (Socket client in SocketServerEx.GetSockets())
                        {
                            string str = client.RemoteEndPoint.ToString();
                                ListBoxItem item = new ListBoxItem();
                                item.Content = str;
                                lbClients.Items.Add(item);
                        }
                    }));
            }
        }
        private void GetDataAndUpdateConsolOutputBox()
        {
            while (running)
            {
                SocketServerEx.GetReceivedDataEvent().WaitOne();
                string str = SocketServerEx.GetData();
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, 
                    new ThreadStart(delegate
                {
                    tbConsoleOutPut.Text = DateTime.Now.ToString("h:mm:ss tt") + 
                    " Client: " + str + tbConsoleOutPut.Text;
                }));

            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbPayLoad.Text)) return;
            try
            {
                tbConsoleOutPut.Text = DateTime.Now.ToString("h:mm:ss tt") + " Server: " + tbPayLoad.Text + "\n" + tbConsoleOutPut.Text;
                SocketServerEx.Send(tbPayLoad.Text);
                tbPayLoad.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
