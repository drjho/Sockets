using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ComLib
{
    public class TcpListnerEx
    {
        TcpListener tcpListner;
        //TcpClient tcpClient;
        StringBuilder strTotal = new StringBuilder();
        string str = string.Empty;
        //StreamWriter outFile = File.CreateText(@"serverlog.txt");
        StreamWriter outFile = new StreamWriter(@"serverlog.txt", true);

        //public AutoResetEvent AllDone { get; set; } = new AutoResetEvent(false);
        public AutoResetEvent ReceivedDataEvent { get; set; } = new AutoResetEvent(false);
        public AutoResetEvent ClientConnectedDisconnectedEvent { get; set; } = new AutoResetEvent(false);

        public List<TcpClient> ClientList { get; set; } = new List<TcpClient>();

        public string GetData() { string tmp = str; str = ""; return tmp; }
        //public AutoResetEvent GetAutoResetEvent() { return autoResetEvent; }
        public void RemoveReadDataFromStrTotal(int count)
        {
            strTotal.Remove(0, count);
        }
        byte[] rx;
        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        public void StartServer(IPAddress ipAddr, int port)
        {
            try
            {
                tcpListner = new TcpListener(ipAddr, port);
                tcpListner.Start();
                outFile.WriteLine("Server Started at: " + DateTime.Now.ToString("h:mm:ss tt"));
                outFile.Flush();
                tcpListner.BeginAcceptTcpClient(onCompleteAcceptTcpClientCallback, tcpListner);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                throw new Exception(ex.Message);
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                throw new Exception(ex.Message);
            }
            catch (System.InvalidOperationException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                throw new Exception(ex.Message);
            }


        }

        public static List<string> GetAllIpAddresses()
        {
            List<string> str = new List<string>();
            string strIP = string.Empty;
            IPHostEntry HostEntry = Dns.GetHostEntry((Dns.GetHostName()));
            if (HostEntry.AddressList.Length > 0)
            {
                foreach (IPAddress ip in HostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) // Tar bara med IPv4 adresser.
                    {
                        strIP = ip.ToString();
                        str.Add(strIP);
                    }
                }
            }
            return str;
        }

        //
        /// <summary>
        /// Callback method is called when a client has been connected
        /// </summary>
        /// <param name="iar"></param>
        void onCompleteAcceptTcpClientCallback(IAsyncResult iar)
        {
            TcpListener tcpl = (TcpListener)iar.AsyncState;
            try
            {
                var tcpClient = tcpl.EndAcceptTcpClient(iar);   // Nu har vi tagit emot en klient.
                ClientList.Add(tcpClient);                      // Lägg klienten till listan.
                ClientConnectedDisconnectedEvent.Set();         // Nu kan man uppdatera Klientlistan på GUI.

                outFile.WriteLine(tcpClient.Client.RemoteEndPoint.ToString());
                outFile.Flush();
                rx = new byte[512];                             // Varför just 512 ??
                tcpClient.GetStream().BeginRead(rx, 0, rx.Length, OnCompleteReadFromTCPClientStreamCallback, tcpClient); // Väntar på klienten ska skicka nåt och när det är läst så starta "OnCompleteReadFromTCPClientStreamCallback".
            }
            catch (Exception exc)
            {

                outFile.WriteLine(exc.Message);
                outFile.Flush();
            }
        }

        /// <summary>
        /// Called when data is recieved
        /// </summary>
        /// <param name="iar"></param>
        void OnCompleteReadFromTCPClientStreamCallback(IAsyncResult iar)
        {
            TcpClient tcpc;
            int countReadBytes = 0;
            string strRecv;
            tcpc = (TcpClient)iar.AsyncState;
            try
            {
                countReadBytes = tcpc.GetStream().EndRead(iar);
                if (countReadBytes == 0)
                {
                    outFile.WriteLine("Client disconnected");
                    outFile.Flush();
                    Debug.WriteLine("Client disconnected");
                    ReceivedDataEvent.Set();
                    return;
                }

                strRecv = Encoding.ASCII.GetString(rx, 0, rx.Length);
                str = strRecv + Environment.NewLine;
                ReceivedDataEvent.Set();
                rx = new byte[1];
                tcpc.GetStream().BeginRead(rx, 0, rx.Length, OnCompleteReadFromTCPClientStreamCallback, tcpc);
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
                // för att loopen inte ska brytas.
                rx = new byte[1];
                tcpc.GetStream().BeginRead(rx, 0, rx.Length, OnCompleteReadFromTCPClientStreamCallback, tcpc);

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            try
            {
                foreach (var client in ClientList)
                {
                    if (client.Client.Connected)
                    {
                        client.GetStream().BeginWrite(data, 0, data.Length, onCompleteWriteToClientStream, client);
                    }
                    else
                    {
                        throw new Exception("No client connected");
                    }
                }
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
                throw new Exception(exc.Message);
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iar"></param>
        void onCompleteWriteToClientStream(IAsyncResult iar)
        {
            try
            {
                TcpClient tcpc = (TcpClient)iar.AsyncState;
                tcpc.GetStream().EndWrite(iar);
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
                Debug.WriteLine(exc.Message);
            }
        }

        public void CloseOutputFile()
        {
            outFile.Close();
        }

    }
}

