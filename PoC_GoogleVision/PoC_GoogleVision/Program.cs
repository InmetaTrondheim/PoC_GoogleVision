using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using Grpc.Auth;
using Image = Google.Cloud.Vision.V1.Image;

namespace PoC_GoogleVision
{
    class Program
    {
        private static bool foundSerial = false;
        private static string serialNumber = "";
        static Stopwatch stopwatch = new Stopwatch();
        static void Main(string[] args)
        {
            var process = Process.GetCurrentProcess();
            string fullPath = GetDirectory();
            char userInput = Console.ReadKey().KeyChar;
            if (char.IsDigit(userInput))
            {
                string path = fullPath + "\\images\\" + userInput  + ".jpg";
                if (File.Exists(path))
                {
                    var image = Image.FromFile(path);
                    var credential = GoogleCredential.FromFile($"{fullPath}\\<FILENAME>.json");

                    var channel = new Grpc.Core.Channel(ImageAnnotatorClient.DefaultEndpoint.Host, credential.ToChannelCredentials());
                    var client = ImageAnnotatorClient.Create(channel);

                    stopwatch.Start();
                    var response = client.DetectTextAsync(image);
                    var response2 = client.DetectDocumentTextAsync(image);

                    Task.WaitAll(response, response2);
                    Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds}ms");

                    if (response2.Result != null)
                    {
                        var splitted = response2.Result.Text.Split('\n');

                        foreach (string text in splitted)
                        {
                            CheckFoundSerial(text);
                            Console.WriteLine($"{text}");
                        }
                    }

                    foreach (var annotation in response.Result)
                    {
                        if (annotation.Description != null)
                        {
                            CheckFoundSerial(annotation.Description);
                            Console.WriteLine(annotation.Description);

                        }
                    }

                    if (foundSerial)
                    {
                        Console.WriteLine($"Found serial number {serialNumber}");
                    }
                    //var base64 = Convert.ToBase64String(File.ReadAllBytes(path));

                    //Task.Run(async () => await Google_Vision_API_Request(base64));
                }
                else
                {
                    Console.WriteLine($"\nImage does not exist");
                }
            }
            else
            {
                Console.WriteLine("\nNot a valid image");
            }

            Console.ReadLine();
        }

        private static void CheckFoundSerial(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Length == 6)
                {
                    if ((text[0] == 'w' || text[0] == 'W'))
                    {
                        string tmpString = text.Substring(1, 5);
                        if (tmpString.All(char.IsDigit))
                        {
                            foundSerial = true;
                            serialNumber = text;
                        }
                    }
                }
            }
        }

        public static async Task Google_Vision_API_Request(string base64string)
        {
            HttpResponseMessage response;
            using (var client = new HttpClient())
            {

                string myJson = $@"{{
                    ""requests"":[
                        {{
                            ""image"":{{
                                ""content"": ""{base64string}""
                            }},
                            ""features"":[
                                {{
                                    ""type"": ""DOCUMENT_TEXT_DETECTION""
                                }}
                            ]
                        }}
                    ]
                }}";

                string requestUri = "https://vision.googleapis.com/v1/images:annotate?key=";
                requestUri += "<INSERTURI>";
                response = await client.PostAsync(
                    requestUri,
                    new StringContent(myJson, Encoding.UTF8, "application/json"));

                var responseStr = await response.Content.ReadAsStringAsync();

                var debug = "";
            }
        }

        private static string GetDirectory()
        {
            return System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }
}
