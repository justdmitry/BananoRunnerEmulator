namespace BananoRunnerEmulator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class Emulator
    {
        private readonly ILogger logger;

        private readonly Uri baseUri = new Uri("http://bbdevelopment.website:27000");

        private readonly HttpClient httpClient;

        private byte bananoCollected = 0;

        private byte bananoMissed = 0;

        private int bananoCollectedTotal = 0;

        public Emulator(ILogger<Emulator> logger)
        {
            this.logger = logger;

            this.httpClient = new HttpClient() { BaseAddress = baseUri };
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UnityPlayer/2018.1.0b13 (UnityWebRequest/1.0, libcurl/7.51.0-DEV)");
            this.httpClient.DefaultRequestHeaders.Accept.TryParseAdd("*/*");
            this.httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("identity");
            this.httpClient.DefaultRequestHeaders.Add("X-Unity-Version", "2018.1.0b13");
        }

        public async Task Run()
        {
            logger.LogInformation("Starting...");

            await GameSettings();
            await AmIValid();
            while (true)
            {
                await GamePacket();
            }
        }

        public async Task GameSettings()
        {
            logger.LogInformation("Sending /gamesettings...");

            using (var req = new HttpRequestMessage(HttpMethod.Get, "/gamesettings"))
            {
                using (var resp = await httpClient.SendAsync(req))
                {
                    resp.EnsureSuccessStatusCode();
                    var respText = await resp.Content.ReadAsStringAsync();
                    logger.LogDebug(respText);
                    Console.WriteLine("Press ENTER to continue...");
                    Console.ReadLine();
                }
            }
        }

        public async Task AmIValid()
        {
            logger.LogInformation("Sending /amivalid...");

            var prms = new Dictionary<string, string>
            {
                ["wallet"] = Program.Wallet,
                ["version"] = "3.0"
            };

            using (var req = new HttpRequestMessage(HttpMethod.Post, "/amivalid"))
            {
                req.Content = new FormUrlEncodedContent(prms);

                using (var resp = await httpClient.SendAsync(req))
                {
                    resp.EnsureSuccessStatusCode();
                    var respText = await resp.Content.ReadAsStringAsync();
                    logger.LogDebug(respText);

                    var respMsg = JsonConvert.DeserializeObject<ServerResponse>(respText);

                    if (respMsg.Code != 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(respMsg.Message);
                        Console.WriteLine($"http://bbdevelopment.website:27000/robochecker&wallet={Program.Wallet}");
                        Console.ResetColor();
                        Console.WriteLine("Press ENTER to continue...");
                        Console.ReadLine();
                    }
                }
            }
        }

        public async Task GamePacket()
        {
            logger.LogInformation($"Sending /gamepacket (collected {bananoCollected}, missed {bananoMissed})...");

            var prms = new Dictionary<string, string>
            {
                ["wallet"] = Program.Wallet,
                ["version"] = "3.0",
                ["collected"] = bananoCollected.ToString(),
                ["missed"] = bananoMissed.ToString(),
            };

            var timeToWait = 0;

            using (var req = new HttpRequestMessage(HttpMethod.Post, "/gamepacket"))
            {
                req.Content = new FormUrlEncodedContent(prms);

                using (var resp = await httpClient.SendAsync(req))
                {
                    resp.EnsureSuccessStatusCode();
                    var respText = await resp.Content.ReadAsStringAsync();
                    logger.LogDebug(respText);

                    var respMsg = JsonConvert.DeserializeObject<ServerResponse>(respText);

                    if (respMsg.Code != 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(respMsg.Message);
                        Console.WriteLine($"http://bbdevelopment.website:27000/robochecker&wallet={Program.Wallet}");
                        Console.ResetColor();
                        Console.WriteLine("Press ENTER to continue...");
                        Console.ReadLine();
                    }

                    bananoCollectedTotal += bananoCollected;

                    var validBananoes = new[] { "2", "3", "4", "5" };
                    var data = respMsg.Block.Text.Split("|");

                    var arr = data.Take(respMsg.Block.Length).Select(x => x.Split(",")).ToList();
                    bananoCollected = 0;
                    bananoMissed = 0;
                    foreach (var scene in arr)
                    {
                        var bananoes = scene.Count(x => validBananoes.Contains(x));
                        switch (bananoes)
                        {
                            case 0:
                                break;
                            case 1:
                                bananoCollected++;
                                break;
                            case 2:
                                bananoCollected++;
                                bananoMissed++;
                                break;
                            case 3:
                                bananoCollected++;
                                bananoMissed += 2;
                                break;
                        }
                    }

                    timeToWait = respMsg.Block.Time + respMsg.Block.Delay;

                    logger.LogInformation($"Total collected: {bananoCollectedTotal}. Next to collect: {bananoCollected}, to miss: {bananoMissed}");
                }
            }

            while (timeToWait > 0)
            {
                await Task.Delay(1000);
                timeToWait--;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"Waiting for {timeToWait}  ");
                Console.CursorLeft = 0;
            }

            Console.ResetColor();
            Console.WriteLine("                           "); // Overwrite "Waiting for..."
        }

        private class ServerResponse
        {
            public byte Code { get; set; }

            public string Message { get; set; }

            public GameBlock Block { get; set; }
        }

        private class GameBlock
        {
            public int Delay { get; set; }

            public int Length { get; set; }

            public int Speed { get; set; }

            public string Text { get; set; }

            public int Time { get; set; }
        }
    }
}
