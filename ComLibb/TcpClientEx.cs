using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace ComLib
{
    public class TcpClientEx
    {
        StreamWriter outFile = new StreamWriter(@"clientlog.txt", true);
        TcpClient tcpClient;
        AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        string str = string.Empty;
        byte[] rx;
        public void Connect(string strIpa, string strPort)
        {
            IPAddress ipa;
            int port;
            try
            {
                if (!IPAddress.TryParse(strIpa, out ipa))
                {
                    throw new Exception("Please supply an IP address.");
                }
                if(!int.TryParse(strPort, out port))
                {
                    port = 23000;
                }
                tcpClient = new TcpClient();
                tcpClient.BeginConnect(ipa, port, onCompleteConnect, tcpClient);
               
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void onCompleteConnect(IAsyncResult iar)
        {
            TcpClient tcpc;
            try
            {
                tcpc = (TcpClient) iar.AsyncState;
                //The asynchronous BeginConnect operation must be completed by calling the EndConnect method. 
                //Typically, the method is invoked by the asyncCallback delegate.
                //This method blocks until the operation is complete.
                tcpc.EndConnect(iar);
                rx = new byte[64];
                tcpClient.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPServerStream, tcpClient);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public void Send(string str)
        {
            byte[] tx;  
            try
            {
                str += "\n";
                tx = Encoding.ASCII.GetBytes(str);
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

        public string GetData()
        {

            lock (str)
            {
                string tmpStr = str;
                str = str.Remove(0, str.Length);
                return tmpStr;
            }

        }

        public AutoResetEvent GetAutoResetEvent()
        {
            return autoResetEvent;
        }

        public void CloseOutputFile()
        {
            outFile.Close();
        }

        private void OnComplateReadFromTCPServerStream(IAsyncResult iar)
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
                    str += strRecv;
                }

                rx = new byte[64];
                tcpc.GetStream().BeginRead(rx, 0, rx.Length, OnComplateReadFromTCPServerStream, tcpc);
            }
            catch (Exception exc)
            {
                outFile.WriteLine(exc.Message);
                outFile.Flush();
            }
        }
            
        
    }

}
