using Microsoft.AspNetCore.Mvc;

using Segerfeldt.EventStore.Refactoring;

namespace RefactoringWebApplication.Controllers;

/// <summary>Provies an API for checking up on the progress of the refactoring.</summary>
[ApiController]
[Route("[controller]")]
public class ProjectionController(IProjectionTracker tracker) : ControllerBase
{
    private readonly ProjectionTracker tracker = (ProjectionTracker)tracker;

    /// <summary>Displays the current position of the projection.</summary>
    [HttpGet]
    public ActionResult<long> GetPosition() => Ok(tracker.Position);
}
