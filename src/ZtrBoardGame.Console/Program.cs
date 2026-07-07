using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;
using Velopack;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.Commands.Setup;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        VelopackApp.Build()
            .Run();

        var (processedArgs, enableConsoleLogging) = args.ProcessGlobalOptions();
        processedArgs = ApplyDefaultWindowsArguments(processedArgs);

        var typeRegistrar = new TypeRegistrar(enableConsoleLogging);
        var app = new CommandApp(typeRegistrar);

        app.Configure(config =>
        {
#if DEBUG
            config.ValidateExamples();
#endif

            config.PropagateExceptions();
            config.SetApplicationName("ZtrBoardGame.Console");
            config.SetHelpProvider(new CustomHelpProvider(config.Settings));

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

            config.AddCommand<SetupCommand>("setup");

            config.SetExceptionHandler((ex, resolver) =>
            {
                if (resolver?.Resolve(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory)
                {
                    var logger = loggerFactory.CreateLogger("UNHANDLED EXCEPTION");
                    logger.LogError(ex, "An unhandled exception occurred during command execution.");
                }
                else
                {
                    Log.Error(ex, "An unhandled exception occurred during command execution and there is no ILoggerFactory!.");
                }

                AnsiConsole.WriteException(ex);
                return -99;
            });

        });

        return await app.RunAsync(processedArgs);
    }

    private static string[] ApplyDefaultWindowsArguments(string[] args)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform
                .Windows))
        {
            return args;
        }

        if (args.Length == 0)
        {
            return ["pc", "run", "--new-ui"];
        }

        return args;
    }
}

