using Spectre.Console;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ZtrBoardGame.RaspberryPi.HardwareAccess.SystemConfigurers;

namespace ZtrBoardGame.Console.Commands.PC;

public class ConfigurePc(IAnsiConsole console) : ISystemConfigurer
{
    public string Name { get; } = "Nazwa komputera";

    public bool CanConfigure()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public Task<bool> IsConfigurationNeededAsync()
    {
        return Task.FromResult(!Environment.MachineName.Equals("pcmr", StringComparison.OrdinalIgnoreCase));
    }

    public async Task ConfigureAsync()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
                Arguments = "-NoProfile -Command \"Rename-Computer -NewName 'pcmr' -Force\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Nie udało się uruchomić procesu PowerShell.");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Operacja zakończyła się błędem (Exit Code: {process.ExitCode}). Upewnij się, że zaakceptowałeś prośbę UAC (uprawnienia administratora).");
            }

            console.MarkupLine("[green]Sukces![/] Zmiana nazwy komputera na 'pcmr' została pomyślnie zainicjowana.");
            console.MarkupLine("[yellow]UWAGA: Aby zmiana weszła w życie, musisz zrestartować komputer![/]");
            console.MarkupLine("Aplikacja zostanie teraz zamknięta.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Wystąpił błąd podczas próby zmiany nazwy komputera: {ex.Message}", ex);
        }
    }
}
