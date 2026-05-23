using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ZtrBoardGame.RaspberryPi.HardwareAccess;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Commands.Setup;

public interface ISystemConfiguratorOrchestrator
{
    IAsyncEnumerable<ISystemConfigurer> GetSystemWhichNeedsConfiguration();
}

internal class SystemConfiguratorOrchestrator(IEnumerable<ISystemConfigurer> systemConfigurers, ILogger<SystemConfiguratorOrchestrator> logger) : ISystemConfiguratorOrchestrator
{
    public async IAsyncEnumerable<ISystemConfigurer> GetSystemWhichNeedsConfiguration()
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
                isConfigNeeded = await configurer.IsConfigurationNeededAsync();
            }
            catch (Exception e)
            {
                throw new SystemConfigurationException($"Cannot check status of {configurer.Name}", e);
            }

            if (!isConfigNeeded)
            {
                continue;
            }

            logger.LogInformation("Hardware configuration for {ConfigurerName} is required.", configurer.Name);

            yield return configurer;
        }
    }
}

