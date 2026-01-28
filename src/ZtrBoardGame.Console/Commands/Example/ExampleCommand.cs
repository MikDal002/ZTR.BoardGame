using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console;

public class ExampleSettings : CommandSettings
{
    [CommandOption("--limit")]
    [Description("Specifies the limit for the operation.")]
    public int? Limit { get; set; } = 4;

    [CommandOption("--directory")]
    [Description("Specifies the target directory.")]
    public DirectoryInfo? Directory { get; set; }

    [CommandOption("--apikey")]
    [Description("The API key for an external service.")]
    [SecretSetting] // Mark this property as secret
    public string? ApiKey { get; set; }
}

public class ExampleCommand(ILogger<ExampleCommand> logger) : CancellableAsyncCommand<ExampleSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ExampleSettings settings, CancellationToken cancellationToken)
    {
        // The LogInterceptor will log settings.ApiKey as [SECRET]
        // This command-specific log now also benefits from that if settings were logged here directly,
        // but the primary sanitization happens in the interceptor.
        logger.LogInformation("Processing ExampleCommand with Limit: {Limit}, Directory: {Directory}, and ApiKey (status): {ApiKeyStatus}",
            settings.Limit,
            settings.Directory?.FullName ?? "Not specified",
            string.IsNullOrEmpty(settings.ApiKey) ? "Not provided" : "Provided");

        var rows = new List<Text>(){
            new($"Limit: {settings.Limit}", new(Color.Red, Color.Black)),
            new($"Directory: {settings.Directory?.FullName ?? "N/A"}", new(Color.Green, Color.Black)),
            new($"API Key: {(string.IsNullOrEmpty(settings.ApiKey) ? "Not provided" : "[SET]")}", new(Color.Yellow, Color.Black)), // Displaying status, not value
            new($"Leftovers: {context.Remaining}", new(Color.Blue, Color.Black))
        };

        AnsiConsole.Write(new Rows(rows));

        if (settings.Directory != null && !settings.Directory.Exists)
        {
            logger.LogWarning("The specified directory {DirectoryPath} does not exist.", settings.Directory.FullName);
        }

        return Task.FromResult(0);
    }
}
