using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace VCLCORISScoreFixer
{
    class Program
    {
       static DateTime _runDate = DateTime.MinValue;
       static string _baseURL = string.Empty;
        static void Main()
        {
            _runDate = DateTime.Now;

            string[] args = Environment.GetCommandLineArgs();
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("┌───────────────────────────────┐");
            Console.WriteLine("│     VCL CORIS Score Fixers    │");
            Console.WriteLine("│             v1.0              │");
            Console.WriteLine("│         Springboard IT        │");
            Console.WriteLine("└───────────────────────────────┘");
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" ");

            if (args.Count() > 1)
            {
                for (int i = 1; i <= args.Count() - 1; i += 2)
                {
                    string lsCommand = args[i];
                    string lsData = args[i + 1];
                    switch (lsCommand.ToUpper())
                    {
                        case "-RUNDATE":
                            DateTime ldValue = DateTime.MinValue;
                            DateTime.TryParse(lsData, out ldValue);
                            _runDate = ldValue;
                            break;
                        case "-URL":
                            _baseURL = lsData;
                            break;
                    }
                }
            }
            RunLive().Wait();
            //for (int i = 0; i < 52; i++)
            //{
            //    Testing().Wait();
            //}

            //Console.ReadKey();
        }

        static async Task RunLive()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            using (var client = new HttpClient(handler))
            {

                client.BaseAddress = new Uri(_baseURL);

                try
                {
                    HttpResponseMessage response = await client.GetAsync("api/ScoreFixing?Date=" + _runDate.ToString("yyyy-MM-dd") + "&CompletedBy=ScoreFixing");
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("{0}", result);
                    }
                    else
                    {
                        Console.WriteLine("{0}", response.ReasonPhrase);
                    }

                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }

            }
        }

        static async Task Testing()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            using (var client = new HttpClient(handler))
            {

                client.BaseAddress = new Uri(_baseURL);

                try
                {
                    _runDate = _runDate.AddDays(7);

                    Console.WriteLine("Running for day: " + _runDate.ToShortDateString());
                    HttpResponseMessage response = await client.GetAsync("api/ScoreFixing?Date=" + _runDate.ToString("yyyy-MM-dd") + "&CompletedBy=ScoreFixing");
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("{0}", result);
                    }
                    else
                    {
                        Console.WriteLine("{0}", response.ReasonPhrase);
                    }

                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }

            }
        }


        ////        HttpClient client = new HttpClient();
        ////        client.BaseAddress = new Uri("http://localhost:54213/api/ScoreFixing?PortID=5069427d-d602-4960-92d7-8fb255982448&Date=2017-01-01&CompletedBy=Mike");

        ////        // Add an Accept header for JSON format.
        ////        client.DefaultRequestHeaders.Accept.Add(
        ////new MediaTypeWithQualityHeaderValue("application/json"));
        ////        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
        ////        System.Text.ASCIIEncoding.ASCII.GetBytes(
        ////            string.Format("{0}:{1}", "landscape-api-user", "Swevuthaq6reguSpefeJabrUguteyEQe"))));
        ////        //// List data response.

        ////        HttpResponseMessage response = client.GetAsync("http://localhost:54213/api/ScoreFixing?PortID=5069427d-d602-4960-92d7-8fb255982448&Date=2017-01-01&CompletedBy=Mike").Result;  // Blocking call!
        ////        string lsRequestMessage = response.RequestMessage.ToString();
        ////        if (response.IsSuccessStatusCode)
        ////        {
        ////            // Parse the response body. Blocking!
        ////            var resp = response.Content.ReadAsStringAsync();
        ////            //var dataObjects = response.Content.ReadAsAsync<IEnumerable<List<DataObject>>>().Result;
        ////            //foreach (var d in dataObjects)
        ////            //{
        ////            //    MessageBox.Show(string.Format("{0}", d.FirstOrDefault().GetText()));
        ////            //}
        ////        }
        ////        //response.EnsureSuccessStatusCode();

    }


       
}

