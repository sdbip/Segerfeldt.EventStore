using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.SQLite;

using System;

namespace ProjectionWebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectionController(IServiceProvider provider) : ControllerBase
{
    private readonly AtomicSQLiteProjectionsTable tracker = (AtomicSQLiteProjectionsTable)provider.GetRequiredKeyedService<IProjectionTracker>("events");

    [HttpGet]
    public ActionResult<long> GetPosition() => Ok(tracker.GetLastFinishedPosition());
}
