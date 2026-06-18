using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OsintController(OsintAgentService agent) : ControllerBase
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpPost("investigate")]
    public async Task InvestigateAsync([FromBody] OsintRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Target))
        {
            Response.StatusCode = 400;
            return;
        }

        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var evt in agent.InvestigateAsync(req.Target, req.TargetType, ct))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(evt, _json)}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
