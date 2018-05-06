namespace BananoRunnerEmulator
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public const string Wallet = "ban_...";

        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddSingleton<Emulator>()
                .BuildServiceProvider();

            var emulator = services.GetRequiredService<Emulator>();

            await emulator.Run();

            await Task.Delay(300);
        }
    }
}
