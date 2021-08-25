using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using static BuildLL.Runtime;
namespace BuildLL
{
    public static class WebHook
    {
        public static bool UseWebhook => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBHOOK_URL"));
        public static void AppveyorDiscordWebhook(string message)
        {
            if (!TryGetEnv("WEBHOOK_URL", out string url)) {
                Console.WriteLine("WEBHOOK_URL not set");
                return;
            }

            Console.WriteLine("Sending webhook...");
            Send(url, new Dictionary<string, string>()
            {
                {"username", "AppVeyor"},
                {
                    "avatar_url",
                    "https://upload.wikimedia.org/wikipedia/commons/thumb/b/bc/Appveyor_logo.svg/256px-Appveyor_logo.svg.png"
                },
                {"content", message}
            });
        }
        //Send JSON
        static void Send(string url, Dictionary<string, string> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var task = client.PostAsJsonAsync(url, data);
                    task.Wait();
                    if (!task.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Task failure");
                        return;
                    }
                    if (!task.Result.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Server error");
                        var errResponse = task.Result.Content.ReadAsStringAsync();
                        errResponse.Wait();
                        Console.WriteLine(errResponse.Result);
                    }
                    else
                    {
                        Console.WriteLine("OK");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception sending webhook");
                Console.WriteLine(e.Message);
            }
        }
    }
}