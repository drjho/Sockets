using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebClientEx
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebClient client = new WebClient())
            {
                // Download string.
                //string value = client.DownloadString("http://en.wikipedia.org/");

                string value = client.DownloadString("http://www.vecka.nu/");
                Console.WriteLine("--- Result ---");
                Console.WriteLine(value.Length);
                Console.WriteLine(value);
            }
            Console.ReadKey();
        }
    }
}
