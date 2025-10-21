using Spectre.Console.Cli;
using System.ComponentModel;

namespace ZtrTemplates.Console.Commands.Base;

public class GlobalCommandSettings : CommandSettings
{
    [CommandOption("--log-console")]
    [Description("Enables logging to the console.")]
    [DefaultValue(false)]
    public bool LogToConsole { get; set; }
}
