using Microsoft.Extensions.DependencyInjection;
using SocuciusErgallaBotv3.Services;
using Serilog;
using Microsoft.Extensions.Logging;

namespace SocuciusErgallaBotv3
{
    internal class Program
    {
        static string _logDirectory = "Logs";
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            if(!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"{_logDirectory}{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            services.AddSingleton<ConfigService>();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<QuoteService>();
            services.AddSingleton<ActivityService>();
            services.AddSingleton<MusicService>();
            services.AddSingleton<MusicTimerService>();
            services.AddSingleton<Bot>();

            var serviceProvider = services.BuildServiceProvider();

            var bot = serviceProvider.GetRequiredService<Bot>();
            await bot.RunAsync();
        }
    }
}