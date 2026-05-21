using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.Commands.Setup;

namespace ZtrBoardGame.Console;

public class SetupSettings : CommandSettings
{
}

public class SetupCommand(IAnsiConsole console, ISystemConfiguratorOrchestrator configuratorOrchestrator, ILogger<SetupCommand> logger) : CancellableAsyncCommand<SetupSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetupSettings settings, CancellationToken cancellationToken)
    {
        var systemWhichNeedsConfiguration = await configuratorOrchestrator.GetSystemsWhichNeedsConfiguration().ToListAsync();

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
            }
        }

        return 0;
    }
}
