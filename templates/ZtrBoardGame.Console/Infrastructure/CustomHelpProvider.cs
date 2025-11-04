using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;
using System.Collections.Generic;
using System.Linq;
using ZtrBoardGame.Console.Commands.Base;

namespace ZtrBoardGame.Console.Infrastructure;

public class CustomHelpProvider : HelpProvider
{
    public CustomHelpProvider(ICommandAppSettings settings)
        : base(settings)
    {
    }

    public override IEnumerable<IRenderable> Write(ICommandModel model, ICommandInfo? command)
    {
        var elements = base.Write(model, command).ToList();

        elements.Add(Text.NewLine);
        elements.Add(new Text("GLOBAL OPTIONS:", new(foreground: Color.Yellow, decoration: Decoration.Bold)));
        elements.Add(Text.NewLine);
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().Padding(2, 0, 2, 0))
            .AddColumn(new());

        var logConsoleOptionName = CommandOptionExtensions.GetLongOptionName<GlobalCommandSettings, bool>(s => s.LogToConsole);
        var logConsoleOptionDescription = CommandOptionExtensions.GetDescription<GlobalCommandSettings, bool>(s => s.LogToConsole);

        grid.AddRow(
            new Text($"  {logConsoleOptionName ?? string.Empty}"),
            new Text(logConsoleOptionDescription ?? string.Empty)
        );

        elements.Add(grid);
        elements.Add(Text.NewLine);

        return elements;
    }
}
