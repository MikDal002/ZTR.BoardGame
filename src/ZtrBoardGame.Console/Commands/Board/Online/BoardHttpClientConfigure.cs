using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Net.Http;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console.Commands.Board.Online;

public static class BoardHttpClientConfigure
{
    public const string ToPcClientName = "ToPcHttpClient";
    public static IServiceCollection ConfigureHelloServiceHttpClient(this IServiceCollection services,
        HttpMessageHandler additionalHttpMessageHandler = null)
    {
        var httpClientBuilder = services.AddHttpClient(ToPcClientName, (serviceProvider, client) =>
        {
            var networkSettings = serviceProvider.GetRequiredService<IOptions<BoardNetworkSettings>>().Value;
            var console = serviceProvider.GetRequiredService<IAnsiConsole>();

            ValidateSettings(networkSettings, console);

            client.BaseAddress = new(networkSettings.PcServerAddress);
            client.Timeout = networkSettings.Timeout;
        });

        if (additionalHttpMessageHandler is not null)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => additionalHttpMessageHandler);
        }

        return services;
    }

    static void ValidateSettings(BoardNetworkSettings networkSettings, IAnsiConsole console)
    {
        if (!string.IsNullOrEmpty(networkSettings.PcServerAddress))
        {
            return;
        }

        const string errorMessage = "PC server address is not configured";
        console.MarkupLine($"[red]Error:[/] {errorMessage}");
        throw new InvalidOperationException(errorMessage);
    }
}
