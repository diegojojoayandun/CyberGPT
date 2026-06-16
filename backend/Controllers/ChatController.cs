using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IRagService rag, SessionService session) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest("Message is required.");

        var sessionId = req.SessionId ?? Guid.NewGuid().ToString();
        var history = await session.GetLastTurnsAsync(sessionId, n: 6);

        var (reply, sources) = await rag.AskAsync(req.Message, history);

        await session.AppendTurnAsync(sessionId, "user", req.Message);
        await session.AppendTurnAsync(sessionId, "assistant", reply);

        return Ok(new ChatResponse(reply, sessionId, sources));
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}
