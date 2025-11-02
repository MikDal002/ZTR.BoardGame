using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ZtrBoardGame.Console.Infrastructure;

public sealed class LogInterceptor : ICommandInterceptor
{
    private readonly ILogger<LogInterceptor> _logger;
    private Stopwatch? _stopwatch;
    private const string SecretMask = "[SECRET]";

    public LogInterceptor(ILogger<LogInterceptor> logger)
    {
        _logger = logger;
    }

    public void Intercept(CommandContext context, CommandSettings? settings)
    {
        _stopwatch = Stopwatch.StartNew();

        var sanitizedSettings = SanitizeSettings(settings);
        _logger.LogInformation("Starting execution of command: {CommandName} with settings: {@CommandSettings}",
            context.Name,
            sanitizedSettings);
    }

    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        _stopwatch?.Stop();
        _logger.LogInformation(
            "Finished execution of command: {CommandName} with result code: {ResultCode}. Duration: {ElapsedDuration}.",
            context.Name,
            result,
            _stopwatch?.Elapsed);
    }

    private static Dictionary<string, object?> SanitizeSettings(CommandSettings? settings)
    {
        if (settings == null)
        {
            return new();
        }

        return settings.GetType()
            .GetProperties()
            .Where(prop => prop.CanRead)
            .ToDictionary(
                prop => prop.Name,
                prop => GetSanitizedPropertyValue(prop, settings) // Call helper method
            );
    }

    private static object? GetSanitizedPropertyValue(PropertyInfo prop, CommandSettings? settings)
    {
        if (prop.GetCustomAttribute<SecretSettingAttribute>() != null)
        {
            return SecretMask;
        }

        var value = prop.GetValue(settings);
        if (value is DirectoryInfo directoryInfo)
        {
            return directoryInfo.FullName;
        }
        // Add more type-specific handling here if needed in the future
        // Example:
        // if (value is FileInfo fileInfo)
        // {
        //     return fileInfo.FullName;
        // }
        return value;
    }
}
