using Microsoft.AspNetCore.Mvc;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IConfiguration config, IHttpClientFactory httpFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var chromaUrl = config["Chroma:BaseUrl"] ?? "http://localhost:8000";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(3);

        var ollamaOk = await PingAsync(http, $"{ollamaUrl}/api/version");
        var chromaOk = await PingAsync(http, $"{chromaUrl}/api/v1/heartbeat");

        return Ok(new
        {
            ollama = ollamaOk ? "up" : "down",
            chroma = chromaOk ? "up" : "down",
            timestamp = DateTime.UtcNow
        });
    }

    private static async Task<bool> PingAsync(HttpClient http, string url)
    {
        try
        {
            var res = await http.GetAsync(url);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
