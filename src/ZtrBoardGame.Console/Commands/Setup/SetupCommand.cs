using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.Setup;

public class SetupSettings : CommandSettings
{
}

public class SetupCommand(IAnsiConsole console, ISystemConfiguratorOrchestrator configuratorOrchestrator, ILogger<SetupCommand> logger) : AsyncCommand<SetupSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SetupSettings settings, CancellationToken cancellationToken)
    {
        var systemWhichNeedsConfiguration = await configuratorOrchestrator.GetSystemsWhichNeedsConfigurationAsync().ToListAsync(cancellationToken);
        var wasError = false;

        if (systemWhichNeedsConfiguration.Count == 0)
        {
            console.MarkupLine("[green]Everything is already configured.[/]");
            return 0;
        }

        foreach (var configurer in systemWhichNeedsConfiguration)
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
                wasError = true;
            }
        }

        return wasError ? 1 : 0;
    }
}
