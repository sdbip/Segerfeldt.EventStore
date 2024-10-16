# Segerfeldt.EventStore.Source.NUnit

A package for assisting tests of Segerfeldt.EventSourcing.Source applications.

It allows for unit testing entities. You can verify added events without needing to publish them:

```csharp
[Test]
public void TestEntityCreationEvent()
{
    var entity = MyEntity.New("creation data");

    // Assert that an event of the specified name was added
    Assert.That(entity, Added.Event("Created"));

    // Assert that the event was added with the correct details
    Assert.That(entity, Added.Event("Created").WithDetails(new CreatedEventDetails("creation data")));
}

[Test]
public void TestEntityOperationEvent()
{
    var entity = new MyEntity(EntityId.Value("some_id").OrThrow(), EntityVersion.New);

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

public sealed class CommandTests
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
    public async Task RegisterUser_Authenticated_Returns204NoContent()
    {
        var response = await client.SendPostCommand("User/", new { username = "user4" },
            h => h.Authorization = new("Username", "test-user"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RegisterUser_NotAuthenticated_Returns401Unauthorized()
    {
        var response = await client.SendPostCommand("User/", new { username = "user4" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
```
