using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;

namespace ZtrBoardGame.Console.Commands.Connectivity;

public class PcSettings : CommandSettings
{
}

public class PcCommand : CancellableAsyncCommand<PcSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PcSettings settings, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers();
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
