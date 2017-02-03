using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComLibb {
    public static class SocketServerEx {
        private static Socket listner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //private static IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
        private static IPAddress ipAddress; // = ipHostInfo.AddressList[1];
        private static Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        static AutoResetEvent allDone = new AutoResetEvent(false);
        static AutoResetEvent receivedDataEvent = new AutoResetEvent(false);
        static AutoResetEvent clientConnectedDisconnectedEvent = new AutoResetEvent(false);

        static List<Socket> socketList = new List<Socket>();
        private static string message;

        public static string GetData() { return message; }

        public static List<Socket> GetSockets() { return socketList; }

        public static AutoResetEvent GetReceivedDataEvent() { return receivedDataEvent; }

        public static AutoResetEvent GetClientConnectedDisconnectedEvent() { return clientConnectedDisconnectedEvent; }

        public static List<string> GetAllIpAddresses()
        {
            List<string> strList = new List<string>();
            string strIp = string.Empty;
            IPHostEntry HostEntry = Dns.GetHostEntry(Dns.GetHostName());
            if (HostEntry.AddressList.Length > 0)
            {
                foreach (IPAddress ip in HostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) // Ta bara med IPv4 addresser
                    {
                        strIp = ip.ToString();
                        strList.Add(strIp);
                    }
                }
            }
            return strList;
        }

        public static string GetData() {
            return message;
        }

        public static string GetIp() {
            return ipAddress.ToString();
        }

        //public static AutoResetEvent GetAutoResetEvent() {
        //    return autoResetEvent;
        //}

        public static void CloseSocket() {
            listner.Close();
        }


        public static void StartListening(string strPort, string strIp) {
            //Databuffer
            ipAddress = IPAddress.Parse(strIp);
            var bytes = new byte[1024];
            var port = int.Parse(strPort);
            var localEndPoint = new IPEndPoint(ipAddress, port);
            //bind socket to local endpoints and listen
            try {
                listner.Bind(localEndPoint);
                listner.Listen(100);
                while (true) {
                    //start async socket listner
                    listner.BeginAccept(new AsyncCallback(AcceptCallback), listner);

                    //wait until connetion is made
                    allDone.WaitOne();
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
            }
        }

        private static void AcceptCallback(IAsyncResult ar) {
            //signal mai to go on
            allDone.Set();
            var listner = (Socket) ar.AsyncState;
            var handler = listner.EndAccept(ar);
            //create state obj
            var state = new StateObject();
            state.workSocket = handler;
            client = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private static void ReadCallback(IAsyncResult ar) {
            var content = string.Empty;
            //get state object and socket from async state obj
            var state = (StateObject) ar.AsyncState;
            var handler = state.workSocket;
            //read data from client
            var bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0) {
                //store recived data
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
            }

            //check for end of file tag
            content = state.sb.ToString();
            var response = string.Empty;
            if (content.IndexOf("\n") > -1) {
                //all dead has been read
                if (content == "time?\r\n") {
                    response = "Response: "+ DateTime.Now.ToString();
                    Send(response);
                }
                else if (content == "course?\r\n") {
                    response = "Response: Network Programming";
                    Send(response);
                }
                else if (content == "name?\r\n") {
                    response = "Response: Viktor Gustafsson";
                    Send(response);
                }
                if (response != "") {
                    message = content + response + "\n";
                }
                else {
                    message = content;
                }
                
                //autoResetEvent.Set();

                //reset state and write data from message
                state.sb.Clear();
                //wait for next message
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            else {
                //not all data recived
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }

        public static void Send(string data) {
            data = "\r\n" + data+"\r\n";
            var msg = Encoding.ASCII.GetBytes(data);
            client.BeginSend(msg, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar) {
            // Retrieve the socket from the state object.
            var handler = (Socket) ar.AsyncState;
            handler.EndSend(ar);
        }
    }
}