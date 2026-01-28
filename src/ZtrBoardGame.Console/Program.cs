using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;
using Velopack;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console;

public static class Program
{
    public static async Task Main(string[] args)
    {
        VelopackApp.Build().Run();

        var (processedArgs, enableConsoleLogging) = args.ProcessGlobalOptions();

        var typeRegistrar = new TypeRegistrar(enableConsoleLogging);
        var app = new CommandApp(typeRegistrar);

        app.Configure(config =>
        {
#if DEBUG
            config.ValidateExamples();
            config.PropagateExceptions();
#endif
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

        await app.RunAsync(processedArgs);
    }
}
