using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Commands.Base;
using ZtrBoardGame.Console.DependencyInjection;

namespace ZtrBoardGame.Console.Commands.Board;

public class BoardRunSettings : CommandSettings
{
}

public class BoardRunCommand : CancellableAsyncCommand<BoardRunSettings>
{
    readonly TypeRegistrar _typeRegistrar;

    public BoardRunCommand(TypeRegistrar typeRegistrar)
    {
        _typeRegistrar = typeRegistrar;
    }

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
        foreach (var copyOfService in _typeRegistrar.GetCopyOfServices())
        {
            builder.Services.Add(copyOfService);
        }

        var serverAddressStorage = new ServerAddressStorage();
        builder.Services.AddSingleton<IServerAddressProvider>(serverAddressStorage);
        builder.Services.AddSingleton<IServerAddressSetter>(serverAddressStorage);

        builder.Services.AddSingleton<IHostedService, HelloService>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        var app = builder.Build();
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var requiredService = app.Services.GetRequiredService<IServer>();
            var serverAddressesFeature = requiredService.Features.Get<IServerAddressesFeature>();
            var serverAddressSetter = app.Services.GetRequiredService<IServerAddressSetter>();
            serverAddressSetter.ServerAddress = serverAddressesFeature?.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("There is no address...");
        });

        app.UseForwardedHeaders();
        app.MapControllers();
        await app.RunAsync(cancellationToken);
        return 0;
    }
}

