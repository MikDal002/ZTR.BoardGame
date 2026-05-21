using Spectre.Console;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.Console.Commands.PC;

public class ConfigurePc(IAnsiConsole console) : ISystemConfigurer
{
    public bool CanConfigure()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public bool IsConfigurationNeeded()
    {
        return !Environment.MachineName.Equals("pcmr", StringComparison.OrdinalIgnoreCase);
    }

    public void Configure()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            console.MarkupLine("[yellow]Automatyczna zmiana nazwy (autofix) nie jest wspierana na tym systemie operacyjnym.[/]");
            console.MarkupLine("Zmień nazwę komputera na [green]pcmr[/] używając komendy np.: [cyan]sudo hostnamectl set-hostname pcmr[/] i uruchom aplikację ponownie.");
            Environment.Exit(0);
        }

        try
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Rename-Computer -NewName 'pcmr' -Force\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Nie udało się uruchomić procesu PowerShell.");
            }

            process.WaitForExit();

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
