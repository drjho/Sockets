using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComLib
{
    public class TcpListnerEx2
    {
        TcpListener tcpListner;
        TcpClient tcpClient;
        StringBuilder strTotal = new StringBuilder();
        string str = string.Empty;
        //StreamWriter outFile = File.CreateText(@"serverlog.txt");
        StreamWriter outFile = new StreamWriter(@"serverlog.txt", true);
        AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        public string GetData()
        {
           
            lock(str)
            {
                string tmpStr = str;
                str = str.Remove(0, str.Length);
                return tmpStr;
            }
           
        }
        public AutoResetEvent GetAutoResetEvent() { return autoResetEvent; }
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
        public bool StartServer(string strIPAddr, string strPort, out string result)
        {
            IPAddress ipaddr;
            int port;
            if (!int.TryParse(strPort, out port))
            {
                port = 23000;
            }
            if (!IPAddress.TryParse(strIPAddr, out ipaddr))
            {
                result = "Invalid IP address supplied.";
                return false;
            }
            try
            {
                tcpListner = new TcpListener(ipaddr, port);
                tcpListner.Start();
                outFile.WriteLine("Server Started at: " + DateTime.Now.ToString("h:mm:ss tt"));
                outFile.Flush();
                tcpListner.BeginAcceptTcpClient(onCompleteAcceptTcpClient, tcpListner);
                result = "Server Started at: " + DateTime.Now.ToString("h:mm:ss tt");
                return true;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                result = ex.Message;
                return false;
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                result = ex.Message;
                return false;
            }
            catch (System.InvalidOperationException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                result = ex.Message;
                return false;
            }


        }
        //
        /// <summary>
        /// Callback method invoked when a client has been connected
        /// </summary>
        /// <param name="iar"></param>
        void onCompleteAcceptTcpClient(IAsyncResult iar)
        {
            TcpListener tcpl = (TcpListener)iar.AsyncState;
            try
            {
                //The asynchronous BeginAcceptTcpClient operation must be completed by calling 
                //the EndAcceptTcpClient method. 
                //Typically, the method is invoked by the asyncCallback delegate.
                //This method blocks until the operation is complete.
                tcpClient = tcpl.EndAcceptTcpClient(iar);
                outFile.WriteLine(tcpClient.Client.RemoteEndPoint.ToString());
                outFile.Flush();
                rx = new byte[64];
                tcpClient.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPClientStream, tcpClient);
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
        void OnComplateReadFromTCPClientStream(IAsyncResult iar)
        {
            TcpClient tcpc;
            int countReadBytes = 0;
            string strRecv;
            try
            {
                tcpc = (TcpClient)iar.AsyncState;
                countReadBytes = tcpc.GetStream().EndRead(iar);
                if (countReadBytes == 0)
                {
                    outFile.WriteLine("Client disconnected");
                    outFile.Flush();
                    Debug.WriteLine("Client disconnected");
                    autoResetEvent.Set();
                    return;
                }

                strRecv = Encoding.ASCII.GetString(rx, 0, rx.Length);
                if (strRecv.IndexOf("\n") > -1)
                {
                    str = strRecv.Substring(0, strRecv.IndexOf("\n") + 1) + str;
                    autoResetEvent.Set();
                }
                else
                {
                    str = strRecv + str;
                }
                
                rx = new byte[64];
                tcpc.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPClientStream, tcpc);
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
                autoResetEvent.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void Send(string message)
        {
            message += "\n";
            byte[] tx =Encoding.ASCII.GetBytes(message);
            try
            {
                if (tcpClient != null)
                {
                    if (tcpClient.Client.Connected)
                    {
                        tcpClient.GetStream().BeginWrite(tx, 0, tx.Length, onCompleteWriteToClientStream, tcpClient);
                    }

                }
                else
                {
                    throw new Exception("No client connected");
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
