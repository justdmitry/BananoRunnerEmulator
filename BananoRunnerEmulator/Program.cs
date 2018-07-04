namespace BananoRunnerEmulator
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false)
               .AddEnvironmentVariables()
               .AddCommandLine(args)
               .Build();

            var options = config.GetSection("EmulatorOptions").Get<EmulatorOptions>();
            var logFileName = $"logs/log.{options.Wallet.Substring(0, 7)}.txt";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, theme: Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme.Grayscale)
                .WriteTo.File(logFileName, Serilog.Events.LogEventLevel.Verbose, fileSizeLimitBytes: 50000000, rollOnFileSizeLimit: true)
                .CreateLogger();

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging(lb => lb.AddSerilog(dispose: true))
                .AddTransient<Emulator>()
                .AddSingleton(options)
                .BuildServiceProvider();

            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

            logger.LogInformation($@"Config: 
   Wallet  {options.Wallet}
     Seed  {options.Seed}
       OS  {options.OS}");

            var emulator = services.GetRequiredService<Emulator>();

            Directory.CreateDirectory("saves");
            var savesFile = $"saves/{options.Wallet}.json";
            if (File.Exists(savesFile))
            {
                var text = await File.ReadAllTextAsync(savesFile);
                emulator.BananoCollectedTotal = int.Parse(text);
                logger.LogInformation("Savefile loaded: TotalBananos = " + emulator.BananoCollectedTotal);
            }

            await emulator.RunAsync(options);

            logger.LogInformation("Emulator stopped.");

            File.WriteAllText(savesFile, emulator.BananoCollectedTotal.ToString());
            logger.LogInformation("Savefile written: TotalBananos = " + emulator.BananoCollectedTotal);
        }
    }
}
