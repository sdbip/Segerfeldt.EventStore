using Microsoft.AspNetCore.Mvc;

using NUnit.Framework;

using Segerfeldt.EventStore.Source.CommandAPI;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

using Require = NUnit.Framework.Assert;

namespace Segerfeldt.EventStore.Source.Tests;

public class CommandResultTests
{
    [Test]
    public void NoValue_ConvertsToActionResultWithoutMessage()
    {
        var result = Ok();
        var actionResult = result.ActionResult();

        Assert.That(result.Content, Is.Null);
        Assert.That(actionResult, Is.TypeOf<StatusCodeResult>());
        Assert.That((actionResult as StatusCodeResult)?.StatusCode, Is.EqualTo(204));
    }

    [Test]
    public void Value_ConvertsToActionResultWithMessage()
    {
        var result = Ok(new {});
        var actionResult = result.ActionResult();

        Assert.That(result.Value, Is.EqualTo(new {}));
        Assert.That(actionResult, Is.TypeOf<ObjectResult>());
        Assert.That((actionResult as ObjectResult)?.StatusCode, Is.EqualTo(200));
        Assert.That((actionResult as ObjectResult)?.Value, Is.EqualTo(new {}));
    }

    [Test]
    public void Error_ConvertsToActionResultWithMessage()
    {
        var result = BadRequest("sample error message");
        var actionResult = result.ActionResult();

        Assert.That(result.Content, Is.EqualTo("sample error message"));
        Assert.That(actionResult, Is.TypeOf<ObjectResult>());
        Assert.That((actionResult as ObjectResult)?.StatusCode, Is.EqualTo(400));
        Assert.That((actionResult as ObjectResult)?.Value, Is.EqualTo("sample error message"));
    }

    [Test]
    public void UntypedError_ChainsToTypedError()
    {
        var initial = BadRequest("sample error message");
        var result = initial.SameErrorFor<string>();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }

    [Test]
    public void TypedError_ChainsToOtherType()
    {
        var initial = BadRequest("sample error message").SameErrorFor<int>();
        var result = initial.SameErrorFor<string>();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }

    [Test]
    public void TypedError_ChainsToUntypedError()
    {
        var initial = BadRequest("sample error message").SameErrorFor<int>();
        var result = initial.SameError();

        Assert.That(result.IsError(), Is.True);
        Assert.That(new {result.StatusCode, result.Content}, Is.EqualTo(new {initial.StatusCode, initial.Content}));
    }
}
