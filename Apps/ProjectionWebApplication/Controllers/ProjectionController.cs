using Microsoft.AspNetCore.Mvc;

namespace ProjectionWebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectionController : ControllerBase
{
    private readonly ProjectionTracker tracker;

    public ProjectionController(ProjectionTracker tracker) => this.tracker = tracker;

    [HttpGet]
    public ActionResult<long> GetPosition() => Ok(tracker.Position);
}
