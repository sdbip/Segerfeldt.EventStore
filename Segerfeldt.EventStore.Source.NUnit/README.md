<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore.Source.NUnit

A package for assisting tests of Segerfeldt.EventSourcing.Source applications.

It allows for unit tests of entities:

```csharp
[Test]
public void TestEntity()
{
    var entity = new TestEntity()
    entity.PerformOperation();

    // Assert that an event of the specified name was added
    Assert.That(entity, Added.Event("EventName"));

    // Assert that the event was added with the correct details
    Assert.That(entity, Added.Event("EventName").WithDetails(new EventDetails("prop1", "prop2")));
}
```

You can also run integration tests to make sure that your commands run correctly.

```csharp
using Segerfeldt.EventStore.Source.NUnit;

namespace SourceWebApplicationTests;

public class CommandTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<SourceWebApplication.AnyClass> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        webApplicationFactory.ClearSourceTables();
    }

    [Test]
    public async Task RegisterUser_Returns204NoContent()
    {
        var response = await client.PostCommand("User/", new { username = "user4" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }
}
```
