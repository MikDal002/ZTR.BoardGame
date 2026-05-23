using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;

namespace ZtrBoardGame.Console.Commands.Setup;

public class HardwareCheckInterceptor(IAnsiConsole console, ISystemConfiguratorOrchestrator systemConfiguratorOrchestrator) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {

        var systemWhichNeedsConfiguration = systemConfiguratorOrchestrator.GetSystemWhichNeedsConfiguration()
            .ToListAsync()
            // Unfortunately, Spectre.Console does not support async interceptors, so we have to block the thread here.
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
