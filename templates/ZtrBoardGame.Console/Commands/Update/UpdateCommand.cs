using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;

namespace ZtrBoardGame.Console;

public class VersionCommandSettings : CommandSettings
{
    [CommandOption("--update")]
    [Description("Checks for a new version and updates if available.")]
    public bool Update { get; set; }
}

public class UpdateCommand(IUpdateService updateService) : CancellableAsyncCommand<VersionCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, VersionCommandSettings settings, CancellationToken cancellationToken)
    {
        var currentVersion = updateService.GetCurrentVersion();
        if (currentVersion is not null)
        {
            AnsiConsole.MarkupLine($"[green]Current version: {currentVersion}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Unable to determine the current version.[/]");
        }

        var newVersion = await AnsiConsole.Status()
            .StartAsync("Checking for updates...",
                async _ => await updateService.CheckForUpdatesAsync());
        if (newVersion is null)
        {
            AnsiConsole.MarkupLine("[green]You are already using the latest version.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[yellow]A new version is available: {newVersion.TargetFullRelease.Version}[/]");

        if (!string.IsNullOrWhiteSpace(newVersion.TargetFullRelease.NotesMarkdown))
        {
            AnsiConsole.WriteLine();
            var panel = new Panel(new Text(newVersion.TargetFullRelease.NotesMarkdown))
                .Header("Release Notes")
                .Border(BoxBorder.Rounded)
                .Expand();
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        if (!settings.Update)
        {
            return 0;
        }

        var confirm = await AnsiConsole.ConfirmAsync("[blue]The application will restart after the update. Do you want to continue?[/]");
        if (!confirm)
        {
            AnsiConsole.MarkupLine("[yellow]Update canceled by the user.[/]");
            return 0;
        }

        await AnsiConsole.Status()
            .StartAsync("Downloading and applying updates...",
                async _ => await updateService.DownloadAndApplyUpdatesAsync(newVersion, cancellationToken));

        AnsiConsole.MarkupLine("[blue]The application will now exit to apply the updates.[/]");

        return 0;
    }
}
