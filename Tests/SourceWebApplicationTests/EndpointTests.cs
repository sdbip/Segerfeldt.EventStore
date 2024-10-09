using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SourceWebApplicationTests;

public class EndpointTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<RegisterUser> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();
    }

    [Test]
    public async Task Entity_Missing_Returns404NotFund()
    {
        var response = await client.GetAsync(new Uri("Entity/2", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
