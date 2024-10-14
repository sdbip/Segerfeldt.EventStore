using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>Builder for configuring an <see cref="EventSource"/></summary>
public sealed class EventSourceBuilder
{
    private readonly List<Func<IServiceProvider, IReceptacle>> receptacles = [];
    private Func<IServiceProvider, IProjectionTracker>? projectionTrackerFunc;
    private readonly Func<IServiceProvider, IEventSourceRepository> getRepository;

    /// <param name="getRepository">A function to </param>
    internal EventSourceBuilder(Func<IServiceProvider, IEventSourceRepository> getRepository)
    {
        this.getRepository = getRepository;
    }

    /// <summary>Add receptacles by searching assemblies</summary>
    /// <param name="assemblies">The assemblies to search</param>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder AddReceptacles(params Assembly[] assemblies)
    {
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
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder AddReceptacle<TReceptacle>() where TReceptacle : IReceptacle =>
        AddReceptacle(provider => ActivatorUtilities.GetServiceOrCreateInstance<TReceptacle>(provider));

    /// <summary>Add a single receptacle</summary>
    /// <param name="receptacle">The receptacle to add</param>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder AddReceptacle(IReceptacle receptacle) =>
        AddReceptacle(_ => receptacle);

    /// <summary>Add a single receptacle</summary>
    /// <param name="receptacleFunc">Function to call to instantiate the receptacle</param>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder AddReceptacle(Func<IServiceProvider, IReceptacle> receptacleFunc)
    {
        receptacles.Add(receptacleFunc);
        return this;
    }

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <typeparam name="TProjectionTracker">The type of the projection tracker</typeparam>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder SetProjectionTracker<TProjectionTracker>() where TProjectionTracker : IProjectionTracker =>
        SetProjectionTracker(provider => provider.GetRequiredService<TProjectionTracker>());

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTracker">The object used for tracking</param>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    // ReSharper disable once ParameterHidesMember
    public EventSourceBuilder SetProjectionTracker(IProjectionTracker projectionTracker) =>
        SetProjectionTracker(_ => projectionTracker);

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTrackerFunc">Function to call to instantiate the position tracker</param>
    /// <returns>This <see cref="EventSourceBuilder"/> for further configuration</returns>
    public EventSourceBuilder SetProjectionTracker(Func<IServiceProvider, IProjectionTracker> projectionTrackerFunc)
    {
        this.projectionTrackerFunc = projectionTrackerFunc;
        return this;
    }

    internal EventSource Build(IServiceProvider provider)
    {
        var eventSource = new EventSource(getRepository(provider), GetProjectionTracker(provider));
        foreach (var receptacle in receptacles)
            eventSource.Register(receptacle(provider));

        return eventSource;
    }

    private IProjectionTracker? GetProjectionTracker(IServiceProvider provider) => projectionTrackerFunc?.Invoke(provider);
}
