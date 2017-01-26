using System.Net.Sockets;
using System.Text;

namespace ComLibb {
    //obj tp read client data async
    class StateObject {
        //Client socket
        public Socket workSocket = null;
        //Buffer size
        public const int BufferSize = 1024;
        //recive buffer
        public byte[] buffer = new byte[BufferSize];
        //recive data
        public StringBuilder sb = new StringBuilder();
    }
}