using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;
using ZtrBoardGame.Console.Commands.Setup;

namespace ZtrBoardGame.Console.Infrastructure;

public class HardwareCheckInterceptor(IAnsiConsole console, ISystemConfiguratorOrchestrator systemConfiguratorOrchestrator) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        var systemWhichNeedsConfiguration = systemConfiguratorOrchestrator.GetSystemsWhichNeedsConfigurationAsync()
            .ToListAsync()
            .GetAwaiter().GetResult();

        if (systemWhichNeedsConfiguration.Count == 0)
        {
            return;
        }

        console.MarkupLine("[yellow]Below systems require configuration:[/]");
        foreach (var configurer in systemWhichNeedsConfiguration)
        {
            console.MarkupLine($"[yellow] - {configurer.Name}[/]");
        }

        console.MarkupLine("[yellow]Run Setup command to run configuration.[/]");
    }
}
