using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI;

/// <summary>Interface that marks a command handler</summary>
/// Do not use this interface directly. Use one of its subinterfaces instead:
/// - <see cref="ICommandHandler{TCommand}"/>
/// - <see cref="ICommandHandler{TCommand, TResponseDTO}"/>
public interface ICommandHandler { }

/// <summary>Interface that marks a command handler without response DTO</summary>
/// <typeparam name="TCommand">DTO type for the command input</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandler
{
    /// <summary>Handles the command</summary>
    /// Access the <see cref="EntityStore"/> and <see cref="EventPublisher"/>
    /// trough the context parameter. The context also includes the
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/> for additional
    /// information about the request.
    /// <param name="command">a DTO that has been deserialized as JSON from the request data</param>
    /// <param name="context">the context of executing the command</param>
    public Task<CommandResult> Handle(TCommand command, CommandContext context);
}

/// <summary>Interface that marks a command handler with response DTO</summary>
/// <typeparam name="TCommand">DTO type for the command input</typeparam>
/// <typeparam name="TResponseDTO">DTO type for the command output</typeparam>
public interface ICommandHandler<in TCommand, TResponseDTO> : ICommandHandler
{
    /// <summary>Handles the command</summary>
    /// Access the <see cref="EntityStore"/> and <see cref="EventPublisher"/>
    /// trough the context parameter. The context also includes the
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/> for additional
    /// information about the request.
    /// <param name="command">a DTO that has been deserialized as JSON from the request data</param>
    /// <param name="context">the context of executing the command</param>
    /// <returns>a dto that will be serialized as JSON in the response data</returns>
    public Task<CommandResult<TResponseDTO>> Handle(TCommand command, CommandContext context);
}

public record EmptyCommand();
