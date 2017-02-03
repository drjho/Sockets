using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComLib
{
    public class SocketClientEx
    {
        // The port number for the remote device.
        private const int port = 23000;
        // Create a TCP/IP socket.
        static Socket client;
        // The response from the remote device.
        static byte[] rx;
        private static String response = String.Empty;
        static List<string> receivedMessage;
        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        public static void Close()
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void StartClient(string strIPAddress, string port)
        {
            try
            {
               
               client = new Socket(AddressFamily.InterNetwork,
                   SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(strIPAddress), int.Parse(port));
                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
            }
            catch (Exception e)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                Debug.WriteLine(e.Message);
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
                Debug.WriteLine("Socket connected to " +
                    client.RemoteEndPoint.ToString());

                rx = new byte[64];
                
                // Wait for the next message.
                client.BeginReceive(rx, 0, rx.Length, 0,
                    new AsyncCallback(OnComplateReadFromTCPServerStream), client);
             
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                
            }
        }

        public static string GetMessage()
        {

            string tmpStr = string.Empty;
            if (receivedMessage.Count > 0)
            {
                tmpStr = receivedMessage[0];
                receivedMessage.RemoveAt(0);
            }
            return tmpStr;
        }

        public static void Send(string str)
        {
            byte[] tx;
            try
            {
                str += "\n";
                tx = Encoding.ASCII.GetBytes(str);
                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.BeginSend(tx, 0, tx.Length, 0, onCompleteWriteToClientStream, client);
                    }
                }
                else
                {
                    throw new Exception("No client connected");
                }
            }
            catch (Exception exc)
            {
             
                throw new Exception(exc.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iar"></param>
        static void onCompleteWriteToClientStream(IAsyncResult iar)
        {

            try
            {
                Socket sc = (Socket)iar.AsyncState;
                sc.EndSend(iar);

            }
            catch (Exception exc)
            {
           
                Debug.WriteLine(exc.Message);
            }

        }

        public static AutoResetEvent GetAutoResetEvent()
        {
            return autoResetEvent;
        }
        private static void OnComplateReadFromTCPServerStream(IAsyncResult ar)
        {
            Socket tcpc;
            int countReadBytes = 0;
            string strRecv;
            try
            {
                tcpc = (Socket)ar.AsyncState;
                countReadBytes = tcpc.EndReceive(ar);
                if (countReadBytes == 0)
                {
                    Debug.WriteLine("Client disconnected");
                    Close();
                    return;
                }

                strRecv = Encoding.ASCII.GetString(rx, 0, rx.Length);
                if (strRecv.IndexOf("\n") > -1)
                {
                    response = strRecv.Substring(0, strRecv.IndexOf("\n") + 1) + response;
                    receivedMessage.Add(response);
                    autoResetEvent.Set();
                }
                else
                {
                    response += strRecv;
                }

                rx = new byte[512];
                client.BeginReceive(rx, 0, rx.Length, SocketFlags.None,
                   new AsyncCallback(OnComplateReadFromTCPServerStream), client);
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }
    }
}
