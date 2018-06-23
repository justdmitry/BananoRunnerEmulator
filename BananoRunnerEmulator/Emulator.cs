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
        private readonly Random rand = new Random();

        private readonly ILogger logger;

        private readonly Uri baseUri = new Uri("http://bbdevelopment.website:27000");

        private readonly HttpClient httpClient;

        private byte bananoCollected = 0;

        private byte bananoMissed = 0;

        private int bananoCollectedTotal = 0;

        private int exceptionsCount = 0;

        public Emulator(ILogger<Emulator> logger)
        {
            this.logger = logger;

            this.httpClient = new HttpClient() { BaseAddress = baseUri };
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UnityPlayer/2018.1.5f1 (UnityWebRequest/1.0, libcurl/7.51.0-DEV)");
            this.httpClient.DefaultRequestHeaders.Accept.TryParseAdd("*/*");
            this.httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("identity");
            this.httpClient.DefaultRequestHeaders.Add("X-Unity-Version", "2018.1.5f1");
        }

        public async Task RunAsync(string wallet)
        {
            logger.LogInformation($"Starting for wallet '{wallet}'");

            while (true)
            {
                try
                {
                    await PlayAsync(wallet);
                    break;
                }
                catch (Exception ex) when (ex is HttpRequestException)
                {
                    exceptionsCount++;
                    if (exceptionsCount > 10)
                    {
                        throw;
                    }

                    var delay = exceptionsCount > 5 ? exceptionsCount * 5 : exceptionsCount;
                    logger.LogWarning($"Exception #{exceptionsCount}, delay {delay} sec: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
            }

            logger.LogInformation("Completed.");
        }

        public async Task PlayAsync(string wallet)
        {
            await GameSettings();

            while (true)
            {
                await GamePacket(wallet);
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
                }
            }
        }

        public async Task GamePacket(string wallet)
        {
            logger.LogDebug($"Sending /gamepacket (collected {bananoCollected}, missed {bananoMissed})...");

            var prms = new Dictionary<string, string>
            {
                ["wallet"] = wallet,
                ["version"] = "4.0",
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
                        logger.LogError("Response: " + respMsg.Message);
                        logger.LogError("If you need validate recaptcha, here is link:");
                        logger.LogError($"  http://bbdevelopment.website:27000/robochecker&wallet={wallet}");
                        logger.LogError("Press ENTER to continue...");
                        Console.ReadLine();
                        logger.LogDebug("ENTER pressed");
                        return;
                    }

                    bananoCollectedTotal += bananoCollected;

                    var validBananoes = new[] { "3", "4", "10" };
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

                    var rnd = rand.Next(0, 100);
                    if (rnd < 15)
                    {
                        bananoCollected--;
                        bananoMissed++;

                        if (rnd < 5)
                        {
                            bananoCollected--;
                            bananoMissed++;
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
                Console.Write($"Waiting for {timeToWait}...  ");
                Console.CursorLeft = 0;
            }

            Console.WriteLine("                              "); // Overwrite "Waiting for..."
            Console.CursorLeft = 0;
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
