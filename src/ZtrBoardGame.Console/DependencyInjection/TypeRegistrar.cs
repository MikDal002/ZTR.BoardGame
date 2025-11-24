using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Commands.Board;
using ZtrBoardGame.Console.Commands.PC;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.DependencyInjection;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public IReadOnlyCollection<ServiceDescriptor> GetCopyOfServices() => _services.ToImmutableList();

    public TypeRegistrar(bool enableConsoleLogging, IServiceCollection? serviceCollection = null)
    {
        _services = serviceCollection ?? new ServiceCollection();

        // --- Configuration Setup ---
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // IF file doesn't exists run _build project first.
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);

        LoggerSetup.ConfigureSerilog(_services, configuration, enableConsoleLogging);
        _services.AddSingleton<ICommandInterceptor, LogInterceptor>();

        _services.Configure<UpdateOptions>(configuration.GetSection(nameof(UpdateOptions)));
        _services.Configure<BoardNetworkSettings>(configuration.GetSection(nameof(BoardNetworkSettings)));
        _services.AddSingleton<IBoardStorage, BoardStorage>();
        _services.AddSingleton<IUpdateService, UpdateService>();
        _services.AddSingleton(AnsiConsole.Console);
        _services.AddSingleton<IBoardStatusStorage, BoardStatusStorage>();
        _services.AddSingleton<IGameService, GameService>();

        _services.AddSingleton<TypeRegistrar>(this);

        _services.ConfigureHelloServiceHttpClient();
    }

    public ITypeResolver Build() => new TypeResolver(_services.BuildServiceProvider());

    public void Register(Type service, Type implementation) => _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) => _services.AddSingleton(service, _ => factory());
}
