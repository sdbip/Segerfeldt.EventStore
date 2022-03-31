using Microsoft.AspNetCore.Mvc;

namespace ProjectionWebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectionController : ControllerBase
{
    private readonly PositionTracker tracker;

    public ProjectionController(PositionTracker tracker) => this.tracker = tracker;

    [HttpGet]
    public ActionResult<long> GetPosition() => Ok(tracker.Position);
}
