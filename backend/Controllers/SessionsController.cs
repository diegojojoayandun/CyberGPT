using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(SessionService session) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SessionInfo>>> GetSessions() =>
        Ok(await session.GetSessionsAsync());

    [HttpGet("{sessionId}/messages")]
    public async Task<ActionResult<List<ChatTurn>>> GetMessages(string sessionId) =>
        Ok(await session.GetAllTurnsAsync(sessionId));

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        await session.DeleteSessionAsync(sessionId);
        return NoContent();
    }
}
