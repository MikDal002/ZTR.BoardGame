using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;
using Velopack.Windows;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
#pragma warning disable CA1416 // Walidacja zgodnoci z platform
        VelopackApp.Build()
            .OnAfterInstallFastCallback((v) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var shortcuts = new Velopack.Windows.Shortcuts();
#pragma warning restore CS0618 // Type or member is obsolete
                shortcuts.CreateShortcut(VelopackLocator.Current.ThisExeRelativePath, ShortcutLocation.Desktop, false, "pc run");
                shortcuts.CreateShortcut(VelopackLocator.Current.ThisExeRelativePath, ShortcutLocation.Desktop, false, "version update");
            })
            .Run();
#pragma warning restore CA1416 // Walidacja zgodnoci z platform

        var (processedArgs, enableConsoleLogging, hardwareConfigurationSettings) = args.ProcessGlobalOptions();

        var typeRegistrar = new TypeRegistrar(enableConsoleLogging, hardwareConfigurationSettings);
        var app = new CommandApp(typeRegistrar);

        app.Configure(config =>
        {
#if DEBUG
            config.ValidateExamples();
#endif

            config.PropagateExceptions();
            config.SetApplicationName("ZtrBoardGame.Console");
            config.SetHelpProvider(new CustomHelpProvider(config.Settings));

            config.AddCommand<ExampleCommand>("commandName");
            config.AddBranch("board", board =>
            {
                board.AddCommand<BoardRunCommand>("run");
            });
            config.AddBranch("pc", pc =>
            {
                pc.AddCommand<PcRunCommand>("run");
            });
            config.AddCommand<UpdateCommand>("version")
                .WithExample("version", "--update");

            config.SetExceptionHandler((ex, _) =>
            {
                Log.Error(ex, "An unhandled exception occurred during command execution.");
                AnsiConsole.WriteException(ex);
                return -99;
            });

        });

        return await app.RunAsync(processedArgs);
    }
}

