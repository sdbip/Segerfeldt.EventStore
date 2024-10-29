using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection;

using System;

namespace ProjectionWebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectionController(IServiceProvider provider) : ControllerBase
{
    private readonly ProjectionTracker tracker = (ProjectionTracker)provider.GetRequiredKeyedService<IProjectionTracker>("events");

    [HttpGet]
    public ActionResult<long> GetPosition() => Ok(tracker.Position);
}
