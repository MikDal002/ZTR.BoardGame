using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private Action<IServiceCollection> _serviceConfiguration;

    public void ConfigureTestServices(Action<IServiceCollection> serviceConfiguration)
    {
        _serviceConfiguration = serviceConfiguration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            _serviceConfiguration?.Invoke(services);
        });
    }
}
