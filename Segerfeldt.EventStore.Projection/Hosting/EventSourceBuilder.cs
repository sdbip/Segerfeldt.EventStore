using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting;

public sealed class EventSourceBuilder
{
    private readonly List<Func<IServiceProvider, IReceptacle>> receptacles = new();
    private Func<IServiceProvider, IPositionTracker>? positionTracker;
    private readonly Func<IServiceProvider, IEventSourceRepository> getRepository;

    public EventSourceBuilder(Func<IServiceProvider, IEventSourceRepository> getRepository)
    {
        this.getRepository = getRepository;
    }

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

    public EventSourceBuilder SetPositionTracker<TPositionTracker>() where TPositionTracker : IPositionTracker =>
        SetPositionTracker(provider => provider.GetRequiredService<TPositionTracker>());

    // ReSharper disable once ParameterHidesMember
    public EventSourceBuilder SetPositionTracker(IPositionTracker positionTracker) =>
        SetPositionTracker(_ => positionTracker);

    public EventSourceBuilder SetPositionTracker(Func<IServiceProvider, IPositionTracker> positionTrackerFunc)
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

    private IPositionTracker? GetPositionTracker(IServiceProvider provider) => positionTracker?.Invoke(provider);
}
