using Microsoft.Extensions.Logging;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

class BootConfigSystemConfigurer(ILogger<BootConfigSystemConfigurer> logger) : ISystemConfigurer
{
    private const string ConfigPath = "/boot/firmware/config.txt";

    private readonly List<(string Key, string Value)> _configBlocks =
    [
        ("arm_freq", "1500"),
        ("arm_freq_min", "300"), // Oszczędność prądu i temperatury przy idle
        // Przesunięcie BT na mini-UART dla zachowania stabilności WiFi na RPi 5
        // (zamiast problematycznego całkowitego wyłączenia)
        ("dtoverlay", "pi3-miniuart-bt"),
        // Wyłączenie inicjalizacji video - RPi headless
        ("hdmi_ignore_hotplug", "1"),
        // Minimalna ilość pamięci GPU dla systemu bez środowiska graficznego
        ("gpu_mem", "16")
    ];

    public static bool CanConfigure()
        => RaspberryPiSystemInfo.IsRaspberryPi();

    public async Task<bool> IsConfigurationNeededAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            logger.LogError("Config file not found: {Path}", ConfigPath);
            return false;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(ConfigPath);
            foreach (var setting in _configBlocks)
            {
                var expectedLine = $"{setting.Key}={setting.Value}";
                if (lines.Any(l => l.Trim() == expectedLine))
                {
                    continue;
                }

                logger.LogInformation("Setting {Setting} is missing or different in {Path}", expectedLine, ConfigPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read config file: {Path}", ConfigPath);
            return false;
        }

        return false;
    }

    public async Task ConfigureAsync()
    {
        try
        {
            logger.LogInformation("Updating Raspberry Pi hardware configuration in {Path}...", ConfigPath);

            var lines = (await File.ReadAllLinesAsync(ConfigPath)).ToList();
            var modified = false;

            foreach (var block in _configBlocks)
            {
                var key = block.Key;
                var value = block.Value;
                var expectedLine = $"{key}={value}";

                var existingLineIndex = lines.FindIndex(l => l.Trim().StartsWith($"{key}="));

                if (existingLineIndex != -1)
                {
                    if (lines[existingLineIndex].Trim() == expectedLine)
                    {
                        continue;
                    }

                    logger.LogInformation("Updating existing setting: {Old} -> {New}", lines[existingLineIndex],
                        expectedLine);
                    lines[existingLineIndex] = expectedLine;
                    modified = true;
                }
                else
                {
                    logger.LogInformation("Adding missing setting block for: {Key}", key);
                    lines.Add(expectedLine);
                    modified = true;
                }
            }

            if (modified)
            {
                await File.WriteAllLinesAsync(ConfigPath, lines);
                logger.LogInformation("Hardware configuration updated successfully. A reboot might be required.");
            }
            else
            {
                logger.LogInformation("Hardware configuration is already up to date.");
            }
        }
        catch (Exception ex)
        {
            throw new SystemConfigurationException($"Failed to update hardware configuration in {ConfigPath}", ex);
        }
    }

    public Task<bool> CanConfigureAsync()
    {
        throw new NotImplementedException();
    }

    public string Name { get; } = "Boot Config";
}
