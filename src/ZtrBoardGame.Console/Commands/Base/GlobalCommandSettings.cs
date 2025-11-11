using Spectre.Console.Cli;
using System.ComponentModel;

namespace ZtrBoardGame.Console.Commands.Base;

public class GlobalCommandSettings : CommandSettings
{
    [CommandOption("--log-console")]
    [Description("Enables logging to the console.")]
    [DefaultValue(false)]
    public bool LogToConsole { get; set; }

    [CommandOption("--testCommandName", IsHidden = true)]
    [Description("The name of the command to be executed.")]

    public string? TestCommandName { get; set; }
}
