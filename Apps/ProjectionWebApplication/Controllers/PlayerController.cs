using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Linq;

namespace ProjectionWebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly ScoreBoard scoreBoard;

    public PlayerController(ScoreBoard scoreBoard)
    {
        this.scoreBoard = scoreBoard;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ScoreDTO>> GetScoreBoard() => Ok(scoreBoard.PlayerScores.Select(s => new ScoreDTO(s.name, s.score)));
}

public record ScoreDTO(string Name, int Score);
