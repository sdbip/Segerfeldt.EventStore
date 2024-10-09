using Microsoft.Extensions.DependencyInjection;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class ServiceLocator(IServiceProvider provider)
{
    private readonly IServiceProvider provider = provider;

    public TService GetServiceOrCreateInstance<TService>() => ActivatorUtilities.GetServiceOrCreateInstance<TService>(provider);
    public object CreateInstance(Type type) => ActivatorUtilities.CreateInstance(provider, type);
}
