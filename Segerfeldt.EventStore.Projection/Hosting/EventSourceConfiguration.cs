using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>Builder for configuring an <see cref="EventSource"/></summary>
public sealed class EventSourceConfiguration(IServiceCollection services, string name)
{
    private readonly IServiceCollection services = services;
    internal readonly string name = name;
    private readonly List<Func<IServiceProvider, IReceptacle>> receptacles = [];

    /// <summary>Add receptacles by searching assemblies</summary>
    /// <param name="assemblies">The assemblies to search</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration AddReceptacles(params Assembly[] assemblies)
    {
        services.TryAddKeyedSingleton(name, (p, n) => new ReceptacleCollection(receptacles.Select(x => x(p))));
        foreach (var assembly in assemblies)
        {
            var types = assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IReceptacle)));
            foreach (var type in types)
                receptacles.Add(provider => (IReceptacle)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));
        }
        return this;
    }

    /// <summary>Add a single receptacle</summary>
    /// <typeparam name="TReceptacle">The type of the receptacle to add; will be injected as needed</typeparam>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration AddReceptacle<TReceptacle>() where TReceptacle : IReceptacle =>
        AddReceptacle(provider => ActivatorUtilities.GetServiceOrCreateInstance<TReceptacle>(provider));

    /// <summary>Add a single receptacle</summary>
    /// <param name="receptacle">The receptacle to add</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration AddReceptacle(IReceptacle receptacle) =>
        AddReceptacle(_ => receptacle);

    /// <summary>Add a single receptacle</summary>
    /// <param name="receptacleFunc">Function to call to instantiate the receptacle</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration AddReceptacle(Func<IServiceProvider, IReceptacle> receptacleFunc)
    {
        services.TryAddKeyedSingleton(name, (p, n) => new ReceptacleCollection(receptacles.Select(x => x(p))));
        receptacles.Add(receptacleFunc);
        return this;
    }

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <typeparam name="TProjectionTracker">The type of the projection tracker</typeparam>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration SetProjectionTracker<TProjectionTracker>() where TProjectionTracker : IProjectionTracker =>
        SetProjectionTracker(provider => provider.GetRequiredService<TProjectionTracker>());

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTracker">The object used for tracking</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    // ReSharper disable once ParameterHidesMember
    public EventSourceConfiguration SetProjectionTracker(IProjectionTracker projectionTracker) =>
        SetProjectionTracker(_ => projectionTracker);

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTrackerFunc">Function to call to instantiate the position tracker</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration SetProjectionTracker(Func<IServiceProvider, IProjectionTracker> projectionTrackerFunc)
    {
        services.AddKeyedSingleton(name, (p, n) => projectionTrackerFunc(p));
        return this;
    }
}
