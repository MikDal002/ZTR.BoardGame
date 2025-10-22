using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Net.Http;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.DependencyInjection;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(bool enableConsoleLogging)
    {
        _services = new ServiceCollection();

        // --- Configuration Setup ---
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // IF file doesn't exists run _build project first.
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);

        LoggerSetup.ConfigureSerilog(_services, configuration, enableConsoleLogging); // Call the new static method
        _services.AddSingleton<ICommandInterceptor, LogInterceptor>();

        _services.Configure<UpdateOptions>(configuration.GetSection(nameof(UpdateOptions)));
        _services.Configure<NetworkSettings>(configuration.GetSection(nameof(NetworkSettings)));
        _services.AddSingleton<IUpdateService, UpdateService>();
        _services.AddSingleton<HttpClient>();
        _services.AddSingleton<HelloService>();
    }

    public ITypeResolver Build() => new TypeResolver(_services.BuildServiceProvider());

    public void Register(Type service, Type implementation) => _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) => _services.AddSingleton(service, _ => factory());
}
