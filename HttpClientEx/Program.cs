using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpClientEx
{
    class Program
    {
        static void Main(string[] args)
        {
            Task<string> t = Task.Run(() => DownloadPageAsync());
            Console.WriteLine("Downloading page...");
            // ... Display the result.
            if (t.Result != null)
            {
                Console.WriteLine(t.Result);
            }

            Console.ReadKey();
        }

        static async Task<string> DownloadPageAsync()
        {
            // ... Target page.
            //string page = "http://en.wikipedia.org/";
            string page = "http://www.vecka.nu/";
            // ... Use HttpClient.
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(page))
                {
                    using (HttpContent content = response.Content)
                    {
                        // ... Read the string.
                        string result = await content.ReadAsStringAsync();
                        return result;

                    }
                }
            }



        }
    }
}
