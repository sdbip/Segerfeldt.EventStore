using Microsoft.Extensions.DependencyInjection;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class ServiceLocator
{
    private readonly IServiceProvider provider;

    public ServiceLocator(IServiceProvider provider)
    {
        this.provider = provider;
    }

    public TService GetServiceOrCreateInstance<TService>() => ActivatorUtilities.GetServiceOrCreateInstance<TService>(provider);
    public object CreateInstance(Type type) => ActivatorUtilities.CreateInstance(provider, type);
}
