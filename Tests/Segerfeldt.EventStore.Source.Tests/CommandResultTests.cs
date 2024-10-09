using Microsoft.AspNetCore.Mvc;

using Segerfeldt.EventStore.Source.CommandAPI;

namespace Segerfeldt.EventStore.Source.Tests;

public sealed class CommandResultTests
{
    [TestCase(199)]
    [TestCase(300)]
    public void IsError(int statusCode)
    {
        Assert.That(CommandResult.Error(statusCode).IsError, Is.True);
    }

    [TestCase(200)]
    [TestCase(299)]
    public void Error_ThrowsIfNotError(int statusCode)
    {
        Assert.That(() => CommandResult.Error(statusCode), Throws.TypeOf<InvalidStatusCodeException>());
    }

    [Test]
    public void NoValue_ConvertsToActionResultWithoutMessage()
    {
        var result = CommandResult.NoContent();
        var actionResult = result.ActionResult();

        Assert.That(result.Content, Is.Null);
        Assert.That(actionResult, Is.TypeOf<StatusCodeResult>());
        Assert.That((actionResult as StatusCodeResult)?.StatusCode, Is.EqualTo(204));
    }

    [Test]
    public void Value_ConvertsToActionResultWithMessage()
    {
        var result = CommandResult.Ok(new {});
        var actionResult = result.ActionResult();

        Assert.That(result.Value, Is.EqualTo(new {}));
        Assert.That(actionResult, Is.TypeOf<OkObjectResult>());
        Assert.That((actionResult as OkObjectResult)?.StatusCode, Is.EqualTo(200));
        Assert.That((actionResult as OkObjectResult)?.Value, Is.EqualTo(new {}));
    }

    [Test]
    public void Error_ConvertsToActionResultWithMessage()
    {
        var result = CommandResult.BadRequest("sample error message");
        var actionResult = result.ActionResult();

        Assert.That(result.Content, Is.EqualTo("sample error message"));
        Assert.That(actionResult, Is.TypeOf<OkObjectResult>());
        Assert.That((actionResult as OkObjectResult)?.StatusCode, Is.EqualTo(400));
        Assert.That((actionResult as OkObjectResult)?.Value, Is.EqualTo("sample error message"));
    }

    [Test]
    public void UntypedError_ChainsToTypedError()
    {
        var initial = CommandResult.BadRequest("sample error message");
        var result = initial.SameErrorFor<string>();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }

    [Test]
    public void TypedError_ChainsToOtherType()
    {
        var initial = CommandResult.BadRequest("sample error message").SameErrorFor<int>();
        var result = initial.SameErrorFor<string>();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }

    [Test]
    public void TypedError_ChainsToUntypedError()
    {
        var initial = CommandResult.BadRequest("sample error message").SameErrorFor<int>();
        var result = initial.SameError();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }
}
