using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console;

public class SetupSettings : CommandSettings
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

public class SetupCommand(IAnsiConsole console, ISystemConfiguratorOrchestrator configuratorOrchestrator, ILogger<SetupCommand> logger) : CancellableAsyncCommand<SetupSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetupSettings settings, CancellationToken cancellationToken)
    {
        var systemWhichNeedsConfiguration = configuratorOrchestrator.GetSystemWhichNeedsConfiguration();

        await foreach (var configurer in systemWhichNeedsConfiguration)
        {
            try
            {
                console.MarkupLine($"Configuring {configurer.Name}...");
                await configurer.ConfigureAsync();

                console.MarkupLine($"[green]{configurer.Name} configured successfully.[/]");
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while configuring {ConfigurerName}.", configurer.Name);
                console.WriteException(e);
            }
        }

        return 0;
    }
}
