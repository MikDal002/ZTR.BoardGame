using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.DependencyInjection;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

public class BoardRunSettings : CommandSettings
{
}

public class BoardRunCommand(TypeRegistrar typeRegistrar) : CancellableAsyncCommand<BoardRunSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, BoardRunSettings runSettings, CancellationToken cancellationToken)
    {
        try
        {
            return await RunWebServer(context, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignore the task canceled exception
        }

        return 0;
    }

    async Task<int> RunWebServer(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());
        AddBoardServices(builder);

        var app = builder.Build();

        app.UseForwardedHeaders();
        app.MapControllers();
        await app.RunAsync(cancellationToken);
        return 0;
    }

    void AddBoardServices(WebApplicationBuilder builder)
    {
        foreach (var copyOfService in typeRegistrar.GetCopyOfServices())
        {
            builder.Services.Add(copyOfService);
        }

        if (builder.Environment.IsE2ETest())
        {
            builder.Services.AddSingleton<IGameStrategy, MockedGameStrategy>();
        }

        builder.Services.AddSingleton<IHostedService, HelloService>();
        builder.Services.AddSingleton<IHostedService, BoardGameService>();
        builder.Services.AddSingleton<IBoardGameStatusStorage, BoardGameStatusStorage>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
    }
}

