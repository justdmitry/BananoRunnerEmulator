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
        private static readonly int[] ValidBananoes = new[] { 3, 4, 10 };

        private readonly Random rand = new Random();

        private readonly ILogger logger;

        private readonly Uri baseUri = new Uri("http://bbdevelopment.website:27000");

        private readonly HttpClient httpClient;

        private int bananoCollected = 0;

        private int bananoMissed = 0;

        private int exceptionsCount = 0;

        private int roundsCount = 0;

        public Emulator(ILogger<Emulator> logger)
        {
            this.logger = logger;

            this.httpClient = new HttpClient() { BaseAddress = baseUri };
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UnityPlayer/2018.2.0b11 (UnityWebRequest/1.0, libcurl/7.52.0-DEV)");
            this.httpClient.DefaultRequestHeaders.Accept.TryParseAdd("*/*");
            this.httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("identity");
            this.httpClient.DefaultRequestHeaders.Add("X-Unity-Version", "2018.2.0b11");
        }

        public int BananoCollectedTotal { get; set; }

        public static int ComputeBananoCollected(List<int[]> data)
        {
            var bonusSums = new[] { 12, 13, 14 };

            var visible = data.Count(x => x.Any(y => ValidBananoes.Contains(y)));

            var bonus = data.Any(x => bonusSums.Contains(x.Sum()));

            return visible + (bonus ? 1 : 0);
        }

        public static int ComputeBananoMissed(List<int[]> data)
        {
            return data
                .Select(x => x.Count(y => ValidBananoes.Contains(y)))
                .Select(x => x > 1 ? x - 1 : 0)
                .Sum();
        }

        public async Task RunAsync(EmulatorOptions options)
        {
            while (true)
            {
                try
                {
                    await PlayAsync(options);
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

        public async Task PlayAsync(EmulatorOptions options)
        {
            logger.LogInformation("PlayAsync() started");

            await GameSettings();

            roundsCount = 0;
            bananoCollected = 0;
            bananoMissed = 0;

            while (true)
            {
                await GamePacket(options);
                roundsCount++;
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

        public async Task GamePacket(EmulatorOptions options)
        {
            logger.LogDebug($"Sending /gamepacket (collected {bananoCollected}, missed {bananoMissed})...");

            var prms = new Dictionary<string, string>
            {
                ["wallet"] = options.Wallet,
                ["version"] = "4.2",
                ["collected"] = bananoCollected.ToString(),
                ["missed"] = bananoMissed.ToString(),
                ["seed"] = options.Seed,
                ["os"] = options.OS,
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
                        logger.LogError($"  http://bbdevelopment.website:27000/robochecker&wallet={options.Wallet}");
                        logger.LogError("Press ENTER to continue...");
                        Console.ReadLine();
                        logger.LogDebug("ENTER pressed");
                        throw new ApplicationException("Need restart round");
                    }

                    exceptionsCount = 0;
                    BananoCollectedTotal += bananoCollected;

                    var data = respMsg.Block.Text.Split("|");
                    var arr = data
                        .Take(respMsg.Block.Length)
                        .Select(x => x.Split(",").Select(y => int.Parse(y)).ToArray())
                        .ToList();
                    bananoCollected = ComputeBananoCollected(arr);
                    bananoMissed = ComputeBananoMissed(arr);

                    var rndMiss = rand.Next(0, 100);
                    if (rndMiss < 15)
                    {
                        bananoCollected--;
                        bananoMissed++;

                        if (rndMiss < 5)
                        {
                            bananoCollected--;
                            bananoMissed++;
                        }
                    }

                    var rndFail = rand.Next(0, 100);
                    if (BananoCollectedTotal < 30 && rndFail < 75)
                    {
                        var delay = rand.Next(5, 10);
                        logger.LogWarning("Fail (newbie), delay " + delay);
                        await Task.Delay(delay * 1000);
                        throw new ApplicationException("Fail (newbie)");
                    }
                    else if (BananoCollectedTotal < 100 && rndFail < 50)
                    {
                        var delay = rand.Next(5, 10);
                        logger.LogWarning("Fail (amateur), delay " + delay);
                        await Task.Delay(delay * 1000);
                        throw new ApplicationException("Fail (amateur)");
                    }
                    else if (BananoCollectedTotal < 1000 && rndFail < 20)
                    {
                        var delay = rand.Next(5, 10);
                        logger.LogWarning("Fail (advanced), delay " + delay);
                        await Task.Delay(delay * 1000);
                        throw new ApplicationException("Fail (advanced)");
                    }

                    timeToWait = respMsg.Block.Time + (roundsCount == 0 ? 0 : respMsg.Block.Delay - 1);

                    logger.LogInformation($"Total collected: {BananoCollectedTotal}. Next to collect: {bananoCollected}, to miss: {bananoMissed}");
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
