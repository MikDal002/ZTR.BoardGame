using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Threading;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Infrastructure;

public class HardwareCheckInterceptor(IAnsiConsole console, IOptions<HardwareConfigurationSettings> config, IEnumerable<ISystemConfigurer> systemConfigurers) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        foreach (var configurer in systemConfigurers)
        {
            if (!configurer.CanConfigure())
            {
                continue;
            }

            var isConfigNeeded = false;
            try
            {
                isConfigNeeded = configurer.IsConfigurationNeeded();
            }
            catch (Exception e)
            {
                console.WriteException(e);
            }

            if (!isConfigNeeded)
            {
                continue;
            }

            console.Write(new Rule("[yellow]Hardware configuration is required.[/]"));

            var confirm = true;
            if (!config.Value.DoAutoConfig)
            {
                confirm = console.Confirm("Do you want to configure the system now?");
            }

            if (confirm)
            {
                try
                {
                    AnsiConsole.Status()
                        .Start($"Configuring {configurer.Name}...", ctx =>
                        {
                            configurer.Configure();
                            Thread.Sleep(500);
                        });

                    console.MarkupLine($"[green]{configurer.Name} configured successfully.[/]");
                }
                catch (Exception e)
                {
                    console.WriteException(e);
                }
            }
            else
            {
                console.MarkupLine("[red]System configuration skipped. The application may not function correctly.[/]");
            }
        }
    }
}
