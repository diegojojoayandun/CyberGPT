using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IRagService rag, IOllamaService ollama, SessionService session) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest("Message is required.");

        var sessionId = req.SessionId ?? Guid.NewGuid().ToString();
        var history = await session.GetLastTurnsAsync(sessionId, n: 6);

        var (reply, sources) = await rag.AskAsync(req.Message, history, req.Category, req.Model);

        await session.AppendTurnAsync(sessionId, "user", req.Message);
        await session.AppendTurnAsync(sessionId, "assistant", reply);

        return Ok(new ChatResponse(reply, sessionId, sources));
    }

    [HttpPost("stream")]
    public async Task StreamAsync([FromBody] ChatRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
        {
            Response.StatusCode = 400;
            return;
        }

        var sessionId = req.SessionId ?? Guid.NewGuid().ToString();
        var history = await session.GetLastTurnsAsync(sessionId, n: 6);

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        // Retrieve RAG context (includes query rewriting + hybrid search + RRF)
        var (sources, context) = await rag.RetrieveAsync(req.Message, history, topK: 5, category: req.Category, model: req.Model);

        // Emit sources metadata first
        var sourcesPayload = sources.Select(s => new { s.FileName, s.Category, s.Content }).ToList();
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { sources = sourcesPayload, sessionId })}\n\n", ct);
        await Response.Body.FlushAsync(ct);

        // Stream tokens
        var fullReply = new StringBuilder();
        await foreach (var token in ollama.StreamAsync(req.Message, context, history, req.Model, ct))
        {
            fullReply.Append(token);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { token })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { done = true })}\n\n", ct);
        await Response.Body.FlushAsync(ct);

        await session.AppendTurnAsync(sessionId, "user", req.Message);
        await session.AppendTurnAsync(sessionId, "assistant", fullReply.ToString());
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}
