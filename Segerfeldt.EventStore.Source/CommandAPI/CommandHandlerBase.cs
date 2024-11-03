using Microsoft.AspNetCore.Http;

using System;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public abstract class CommandHandlerBase
{
    /// <summary>The command context</summary>
    public CommandContext Context { get; private set; } = null!;
    /// <summary>The HTTP context with access to the <see cref="HttpRequest"/> object</summary>
    public HttpContext HttpContext => Context.HttpContext;
    /// <summary>The <see cref="EntityStore"/> for accessing persisted entities</summary>
    public EntityStore EntityStore => Context.EntityStore;
    /// <summary>
    ///     The <see cref="EventPublisher"/> for advanced publishing of events.
    ///     Prefer <see cref="PublishChangesAsync(IEntity[])"/> which autimatically assigns the actor.
    /// </summary>
    public EventPublisher EventPublisher => Context.EventPublisher;
    /// <summary>The username of the authenticated user</summary>
    public string Actor => Context.HttpContext.User.Identity?.Name!;

    protected async Task<object> Execute<TCommandDTO, TResult>(TCommandDTO command, CommandContext context, Func<TCommandDTO, Task<TResult>> x) where TResult : class
    {
        Context = context;
        // Return 401 UNAUTHORIZED if the user cannot be idetified securely.
        if (Actor is null) return CommandResult.Unauthorized();
        return await x(command);
    }

    /// <summary>Publish changes to modified entities</summary>
    /// <param name="entities">The modified entities</param>
    protected async Task PublishChangesAsync(params IEntity[] entities)
    {
        await EventPublisher.PublishChangesAsync(entities, Actor);
    }
}

/// <summary>A base class for implementing <see cref="ICommandHandler{TCommandDTO}" /></summary>
public abstract class CommandHandlerBase<TCommandDTO> : CommandHandlerBase, ICommandHandler<TCommandDTO>
{
    /// <inheritdoc/>
    public async Task<CommandResult> Handle(TCommandDTO command, CommandContext context) =>
        CommandResult.Cast(await Execute(command, context, Execute))!;

    /// <summary>Implement this abstract method to handle the command.</summary>
    /// <param name="command">The command DTO</param>
    protected abstract Task<CommandResult> Execute(TCommandDTO command);
}

/// <summary>A base class for implementing <see cref="ICommandHandler{TCommandDTO, TResponseDTO}" /></summary>
public abstract class CommandHandlerBase<TCommandDTO, TResponseDTO> : CommandHandlerBase, ICommandHandler<TCommandDTO, TResponseDTO> where TResponseDTO : class
{
    /// <inheritdoc/>
    public async Task<CommandResult<TResponseDTO>> Handle(TCommandDTO command, CommandContext context) =>
        CommandResult<TResponseDTO>.Cast(await Execute(command, context, Execute))!;

    /// <summary>Implement this abstract method to handle the command.</summary>
    /// <param name="command">The command DTO</param>
    protected abstract Task<CommandResult<TResponseDTO>> Execute(TCommandDTO command);
}
