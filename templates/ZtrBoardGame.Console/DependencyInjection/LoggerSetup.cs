using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.IO;

namespace ZtrTemplates.Console.DependencyInjection;

public static class LoggerSetup
{
    public static void ConfigureSerilog(IServiceCollection services, IConfiguration configuration, bool enableConsoleLogging)
    {
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        var logFilePath = Path.Combine(logDirectory, "app.log.json");

        Directory.CreateDirectory(logDirectory);

        if (File.Exists(logFilePath))
        {
            try
            {
                File.Delete(logFilePath);
            }
            catch (IOException ex)
            {
                System.Console.Error.WriteLine($"[ERROR] Could not delete existing log file '{logFilePath}': {ex.Message}");
            }
        }

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        if (enableConsoleLogging)
        {
            loggerConfiguration
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information // Or another level if you want console to be less/more verbose
            );
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
    }
}
