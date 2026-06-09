using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Threading;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Infrastructure;

class HardwareCheckInterceptor(IAnsiConsole console, IOptions<HardwareConfigurationSettings> config, IEnumerable<ISystemConfigurer> systemConfigurers) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is not BoardRunSettings)
        {
            return;
        }

        foreach (var systemConfigurer in systemConfigurers)
        {

            var isConfigNeeded = false;
            try
            {
                isConfigNeeded = systemConfigurer.IsConfigurationNeeded();
            }
            catch (Exception e)
            {
                console.WriteException(e);
            }

            if (!isConfigNeeded)
            {
                return;
            }

            console.Write(new Rule($"[yellow]Hardware configuration for {systemConfigurer.Name} is required.[/]"));

            var confirm = true;
            if (!config.Value.DoAutoConfig)
            {
                confirm = console.Confirm($"Do you want to configure the {systemConfigurer.Name} now?");
            }

            if (confirm)
            {
                try
                {
                    AnsiConsole.Status()
                        .Start("Configuring RaspberryPi...", ctx =>
                        {
                            systemConfigurer.Configure();
                            Thread.Sleep(500);
                        });

                    console.MarkupLine($"[green]{systemConfigurer.Name} configured successfully.[/]");
                }
                catch (Exception e)
                {
                    console.WriteException(e);
                }
            }
            else
            {
                console.MarkupLine($"[red]{systemConfigurer.Name} configuration skipped. The application may not function correctly.[/]");
            }
        }
    }
}
