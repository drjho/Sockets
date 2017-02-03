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
        TcpClient tcpClient;
        StringBuilder strTotal = new StringBuilder();
        string str = string.Empty;
        //StreamWriter outFile = File.CreateText(@"serverlog.txt");
        StreamWriter outFile = new StreamWriter(@"serverlog.txt", true);
        AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        public string GetData() { return str; }
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
        public void StartServer(IPAddress ipAddr, int port)
        {
            try
            {
                tcpListner = new TcpListener(ipAddr, port);
                tcpListner.Start();
                outFile.WriteLine("Server Started at: " + DateTime.Now.ToString("h:mm:ss tt"));
                outFile.Flush();
                tcpListner.BeginAcceptTcpClient(onCompleteAcceptTcpClient, tcpListner);
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                outFile.WriteLine(ex.Message);
                outFile.Flush();
                throw new Exception(ex.Message);
            }
            catch(System.ArgumentOutOfRangeException ex)
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
        //
        /// <summary>
        /// Callback method is called when a client has been connected
        /// </summary>
        /// <param name="iar"></param>
        void onCompleteAcceptTcpClient(IAsyncResult iar)
        {
            TcpListener tcpl = (TcpListener)iar.AsyncState;
            try
            {
                tcpClient = tcpl.EndAcceptTcpClient(iar);
                // 
                outFile.WriteLine(tcpClient.Client.RemoteEndPoint.ToString());
                outFile.Flush();
                rx = new byte[512];
                tcpClient.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPClientStream, tcpClient);
            }
            catch(Exception exc)
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
                if(countReadBytes == 0)
                {
                    //;
                    outFile.WriteLine("Client disconnected");
                    outFile.Flush();
                    Debug.WriteLine("Client disconnected");
                    autoResetEvent.Set();
                    return;
                }

                strRecv = Encoding.ASCII.GetString(rx, 0, rx.Length);
                str = strRecv + Environment.NewLine;
                autoResetEvent.Set();
                rx = new byte[1];
                tcpc.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPClientStream, tcpc);
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
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
                if (tcpClient != null)
                {
                    if (tcpClient.Client.Connected)
                    {
                        tcpClient.GetStream().BeginWrite(data, 0, data.Length, onCompleteWriteToClientStream, tcpClient);
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
