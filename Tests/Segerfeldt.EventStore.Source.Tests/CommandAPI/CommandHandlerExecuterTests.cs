using Microsoft.AspNetCore.Mvc;

using NUnit.Framework;

using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Tests.CommandAPI;

public sealed class CommandHandlerExecuterTests
{
    [Test]
    public async Task ExecutesCommandHandler()
    {
        var executer = new CommandHandlerExecuter(new CommandHandler());
        var context = new CommandContext { HttpContext = null!, EntityStore = null!, EventPublisher = null!, Request = null! };
        var response = (StatusCodeResult)await executer.ExecuteHandlerAsync(new EmptyCommand(), context);
        Assert.That(response.StatusCode, Is.EqualTo(204));
    }

    [Test]
    public async Task ExecutesCommandHandlerWithResponseDTO()
    {
        var executer = new CommandHandlerExecuter(RespondingCommandHandler.WithResponseValue(42));
        var context = new CommandContext { HttpContext = null!, EntityStore = null!, EventPublisher = null!, Request = null! };
        var response = (OkObjectResult)await executer.ExecuteHandlerAsync(new EmptyCommand(), context);
        Assert.That(response.StatusCode, Is.EqualTo(200));
        Assert.That(response.Value, Is.EqualTo(new ResponseDTO(42)));
    }

    [Test]
    public async Task Returns500InternalServerErrorIfCommandHandlerThrows()
    {
        var executer = new CommandHandlerExecuter(new ThrowingCommandHandler());
        var context = new CommandContext { HttpContext = null!, EntityStore = null!, EventPublisher = null!, Request = null! };
        var response = (ObjectResult)await executer.ExecuteHandlerAsync(new EmptyCommand(), context);
        Assert.That(response.StatusCode, Is.EqualTo(500));
    }

    private class CommandHandler : ICommandHandler<EmptyCommand>
    {
        public Task<CommandResult> Handle(EmptyCommand command, CommandContext context) =>
            Task.FromResult(CommandResult.NoContent());
    }

    private class ThrowingCommandHandler : ICommandHandler<EmptyCommand>
    {
        public Task<CommandResult> Handle(EmptyCommand command, CommandContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    private class RespondingCommandHandler : ICommandHandler<EmptyCommand, ResponseDTO>
    {
        private readonly int responseValue;

        private RespondingCommandHandler(int responseValue)
        {
            this.responseValue = responseValue;
        }

        public static RespondingCommandHandler WithResponseValue(int responseValue) => new(responseValue);

        public Task<CommandResult<ResponseDTO>> Handle(EmptyCommand command, CommandContext context) =>
            Task.FromResult(CommandResult.Ok(new ResponseDTO(responseValue)));
    }

    private record EmptyCommand();
    private record ResponseDTO(int value);
}
