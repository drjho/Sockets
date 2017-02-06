using ComLib;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SocketClientUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool running = true;
        static bool taskStarted = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetDataAndUpdateConsolOutputBox()
        {
            while (running)
            {
                SocketClientEx.GetAutoResetEvent().WaitOne();
                string str = SocketClientEx.GetMessage();
                int size = str.Length;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    tbConsole.Text = DateTime.Now.ToString("h:mm:ss tt") + " Server: " + str + tbConsole.Text;
                }));

            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbServerIP.Text) || string.IsNullOrEmpty(tbServerPort.Text))
                {
                    return;
                }
                SocketClientEx.StartClient(tbServerIP.Text, tbServerPort.Text);
                if (!taskStarted)
                {
                    Task t = new Task(GetDataAndUpdateConsolOutputBox);
                    t.Start();
                    taskStarted = true;
                }
                lbStatus.Content = "Connected";
                btnConnect.IsEnabled = false;
                btnSend.IsEnabled = true;
                btnExit.IsEnabled = true;
                
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbPayLoad.Text)) return;
            try
            {
                tbConsole.Text = DateTime.Now.ToString("h:mm:ss tt") + " Client: " + tbPayLoad.Text + "\n" + tbConsole.Text;
                SocketClientEx.Send(tbPayLoad.Text);
                tbPayLoad.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            SocketClientEx.Close();
            btnSend.IsEnabled = false;
            btnExit.IsEnabled = false;
            btnConnect.IsEnabled = true;
        }
    }
}
