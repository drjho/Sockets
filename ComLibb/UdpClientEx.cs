using System;
using System.Net.Sockets;
using System.Text;

namespace ComLib
{
    class UdpClientEx
    {
        UdpClient udpClient;
        private void ConnectAndSend(string hostname)
        { UdpClient udpClient = new UdpClient();
            udpClient.Connect(hostname, 23000);
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Hello World?");
            udpClient.Send(sendBytes, sendBytes.Length); }

    }
}
