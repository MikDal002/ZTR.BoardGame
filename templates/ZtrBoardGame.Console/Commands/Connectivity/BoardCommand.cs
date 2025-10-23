using Spectre.Console.Cli;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;

namespace ZtrBoardGame.Console.Commands.Connectivity;

public class BoardSettings : CommandSettings
{
}

public class BoardCommand : CancellableAsyncCommand<BoardSettings>
{
    private readonly IHelloService _helloService;

    public BoardCommand(IHelloService helloService)
    {
        _helloService = helloService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, BoardSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _helloService.AnnouncePresence(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignore the task canceled exception
        }

        return 0;
    }
}
