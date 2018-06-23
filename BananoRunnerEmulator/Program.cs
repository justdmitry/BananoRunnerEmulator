namespace BananoRunnerEmulator
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, theme: Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme.Grayscale)
                .WriteTo.File("log.txt", Serilog.Events.LogEventLevel.Verbose, fileSizeLimitBytes: 5000000, rollOnFileSizeLimit: true)
                .CreateLogger();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging(lb => lb.AddSerilog(dispose: true))
                .AddTransient<Emulator>()
                .BuildServiceProvider();

            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

            var wallet = config["Wallet"];

            logger.LogInformation($"Config: Wallet '{wallet}'");

            var emulator = services.GetRequiredService<Emulator>();

            await emulator.RunAsync(wallet);

            logger.LogInformation("DONE.");
        }
    }
}
