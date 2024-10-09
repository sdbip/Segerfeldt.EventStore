using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting;

public sealed class EventSourceBuilder(Func<IServiceProvider, IEventSourceRepository> getRepository)
{
    private readonly List<Func<IServiceProvider, IReceptacle>> receptacles = new();
    private Func<IServiceProvider, IProjectionTracker>? positionTracker;
    private readonly Func<IServiceProvider, IEventSourceRepository> getRepository = getRepository;

    public EventSourceBuilder AddReceptacles(Assembly assembly)
    {
        var types = assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IReceptacle)));
        foreach (var type in types)
            receptacles.Add(provider => (IReceptacle)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));
        return this;
    }

    public EventSourceBuilder AddReceptacle<TReceptacle>() where TReceptacle : IReceptacle =>
        AddReceptacle(provider => provider.GetRequiredService<TReceptacle>());

    public EventSourceBuilder AddReceptacle(IReceptacle receptacle) =>
        AddReceptacle(_ => receptacle);

    public EventSourceBuilder AddReceptacle(Func<IServiceProvider, IReceptacle> receptacleFunc)
    {
        receptacles.Add(receptacleFunc);
        return this;
    }

    public EventSourceBuilder SetProjectionTracker<TPositionTracker>() where TPositionTracker : IProjectionTracker =>
        SetProjectionTracker(provider => provider.GetRequiredService<TPositionTracker>());

    // ReSharper disable once ParameterHidesMember
    public EventSourceBuilder SetProjectionTracker(IProjectionTracker positionTracker) =>
        SetProjectionTracker(_ => positionTracker);

    public EventSourceBuilder SetProjectionTracker(Func<IServiceProvider, IProjectionTracker> positionTrackerFunc)
    {
        positionTracker = positionTrackerFunc;
        return this;
    }

    internal EventSource Build(IServiceProvider provider)
    {
        var eventSource = new EventSource(getRepository(provider), GetPositionTracker(provider));
        foreach (var receptacle in receptacles)
            eventSource.Register(receptacle(provider));

        return eventSource;
    }

    private IProjectionTracker? GetPositionTracker(IServiceProvider provider) => positionTracker?.Invoke(provider);
}
