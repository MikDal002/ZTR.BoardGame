using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.DependencyInjection;

namespace ZtrBoardGame.Console.Commands.PC;

public class PcRunSettings : CommandSettings
{
}

public class PcRunCommand(TypeRegistrar typeRegistrar) : CancellableAsyncCommand<PcRunSettings>
{

    public override async Task<int> ExecuteAsync(CommandContext context, PcRunSettings runSettings, CancellationToken cancellationToken)
    {
        return await RunWebServer(context, cancellationToken);
    }

    async Task<int> RunWebServer(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());
        foreach (var copyOfService in typeRegistrar.GetCopyOfServices())
        {
            builder.Services.Add(copyOfService);
        }

        builder.Services.AddSingleton<IHostedService, BoardConnectionCheckerService>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        var app = builder.Build();

        app.UseForwardedHeaders();
        app.MapControllers();
        await app.RunAsync(cancellationToken);
        return 0;
    }
}
