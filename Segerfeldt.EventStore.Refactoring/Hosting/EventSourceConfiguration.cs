using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Refactoring.Hosting;

/// <summary>Builder for configuring an <see cref="EventSource"/></summary>
public sealed class EventSourceConfiguration(IServiceCollection services)
{
    public EventSourceConfiguration UseTarget(Func<IServiceProvider, IDbConnection> target)
    {
        services.AddSingleton(p => new EventPublisher(target(p)));
        return this;
    }

    /// <summary>Use a prepared object to transform events</summary>
    /// <param name="strategy">The transformation stategy object</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration UseTransformationStrategy(ITransformationStrategy strategy)
    {
        services.AddSingleton(strategy);
        return this;
    }

    /// <summary>Use injection to create the object that transforms events</summary>
    /// <typeparam name="TTransformationStrategy">The type that implements the transformation</typeparam>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration UseTransformationStrategy<TTransformationStrategy>() where TTransformationStrategy : ITransformationStrategy =>
        UseTransformationStrategy(provider => ActivatorUtilities.GetServiceOrCreateInstance<TTransformationStrategy>(provider));

    /// <summary>Use a factory function to create the transformation object after app is launched</summary>
    /// <param name="transformationStrategyFactory">Function to call to instantiate the receptacle</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration UseTransformationStrategy(Func<IServiceProvider, ITransformationStrategy> transformationStrategyFactory)
    {
        services.AddSingleton(transformationStrategyFactory);
        return this;
    }

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <typeparam name="TProjectionTracker">The type of the projection tracker</typeparam>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration UseProjectionTracker<TProjectionTracker>() where TProjectionTracker : IProjectionTracker =>
        UseProjectionTracker(provider => provider.GetRequiredService<TProjectionTracker>());

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTracker">The object used for tracking</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    // ReSharper disable once ParameterHidesMember
    public EventSourceConfiguration UseProjectionTracker(IProjectionTracker projectionTracker) =>
        UseProjectionTracker(_ => projectionTracker);

    /// <summary>Set the <see cref="IProjectionTracker"/> used to persist the position</summary>
    /// <param name="projectionTrackerFunc">Function to call to instantiate the position tracker</param>
    /// <returns>This <see cref="EventSourceConfiguration"/> for further configuration</returns>
    public EventSourceConfiguration UseProjectionTracker(Func<IServiceProvider, IProjectionTracker> projectionTrackerFunc)
    {
        services.AddSingleton(projectionTrackerFunc);
        return this;
    }
}
