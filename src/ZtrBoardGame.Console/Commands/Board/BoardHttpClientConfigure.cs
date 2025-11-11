using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Net.Http;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.Console.Commands.Board;

public static class BoardHttpClientConfigure
{
    public const string ToPcClientName = "ToPcHttpClient";
    public static IServiceCollection ConfigureHelloServiceHttpClient(this IServiceCollection services,
        HttpMessageHandler httpMessageHandlerMock = null)
    {
        var httpClientBuilder = services.AddHttpClient(ToPcClientName, (serviceProvider, client) =>
        {
            var networkSettings = serviceProvider.GetRequiredService<IOptions<NetworkSettings>>().Value;
            var console = serviceProvider.GetRequiredService<IAnsiConsole>();

            if (string.IsNullOrEmpty(networkSettings.PcServerAddress))
            {
                const string errorMessage = "PC server address is not configured";
                console.MarkupLine($"[red]Error:[/] {errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }

            client.BaseAddress = new(networkSettings.PcServerAddress);
        });

        if (httpMessageHandlerMock is not null)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandlerMock);
        }

        return services;
    }
}
