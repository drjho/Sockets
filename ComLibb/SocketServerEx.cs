using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ComLib
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class SocketServerEx
    {
        static Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static AutoResetEvent allDone = new AutoResetEvent(false);
        static AutoResetEvent receivedDataEvent = new AutoResetEvent(false);
        static AutoResetEvent clientConnectedDisconnectedEvent = new AutoResetEvent(false);

        static List<Socket> sockets = new List<Socket>();
        static string message;
        public static string GetData() { return message; }

        public static List<Socket> GetSockets() { return sockets; }

        public static AutoResetEvent GetReceivedDataEvent() { return receivedDataEvent; }
        public static AutoResetEvent GetClientConnectedDisconnectedEvent() { return clientConnectedDisconnectedEvent; }


        public static void CloseSocket()
        {
            listener.Shutdown(SocketShutdown.Both);
            listener.Close();
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
        public static void Send(string str)
        {
            byte[] tx;
            try
            {
                str += "\n";
                tx = Encoding.ASCII.GetBytes(str);
                foreach (Socket clientSend in GetSockets())
                {
                    try
                    {
                        if (clientSend.Connected)
                        {
                            Debug.WriteLine(clientSend.SendTimeout);
                            //clientSend.Send(tx);
                            clientSend.BeginSend(tx, 0, tx.Length, 0, onCompleteWriteToClientStream, clientSend);
                        }
                    }
                    catch
                    {
                        // If the send fails the close the connection
                        clientSend.Close();
                        GetSockets().Remove(clientSend);
                    }
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
        //[Obsolete("Single client only")]
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strPort"></param>
        /// <param name="strIPAdress"></param>
        public static void StartListening(string strPort, string strIPAdress)
        {
            int port = int.Parse(strPort);
            IPAddress ipAddress = IPAddress.Parse(strIPAdress);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                while (true)
                {
                    // Start an asynchronous socket to listen for connections.
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket client = listener.EndAccept(ar);
            sockets.Add(client);
            clientConnectedDisconnectedEvent.Set();
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the client socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            // Read data from the client socket. 
            int bytesRead = client.EndReceive(ar);
            if (bytesRead == 0)
            {
                // Telling ListBox to remove the client from the list of clients
                sockets.Remove(client);
                clientConnectedDisconnectedEvent.Set();
                return;
            }
            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));
                // Check for end-of-line tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("\n") > -1)
                {
                    // All the data has been read from the 
                    message = content + message;
                    receivedDataEvent.Set();
                    // Reset your state.sb      
                    state.sb.Clear();
                }
                // Wait for the next message.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }
    }
}
