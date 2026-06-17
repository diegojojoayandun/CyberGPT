using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CyberGPT.API.Controllers;

public record ModelInfo(string Name, long SizeBytes, string SizeLabel, DateTime ModifiedAt);

[ApiController]
[Route("api/[controller]")]
public class ModelsController(IConfiguration config, IHttpClientFactory httpFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ModelInfo>>> GetModels()
    {
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var res = await http.GetAsync($"{baseUrl}/api/tags");
            if (!res.IsSuccessStatusCode)
                return StatusCode(502, "Ollama no disponible");

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var models = doc.RootElement.GetProperty("models").EnumerateArray()
                .Select(m =>
                {
                    var name = m.GetProperty("name").GetString() ?? "";
                    var size = m.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0L;
                    var modified = m.TryGetProperty("modified_at", out var mod)
                        ? DateTime.Parse(mod.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;
                    return new ModelInfo(name, size, FormatSize(size), modified);
                })
                .OrderBy(m => m.Name)
                .ToList();

            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "No se pudo conectar a Ollama", detail = ex.Message });
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "?";
        if (bytes >= 1_000_000_000) return $"{bytes / 1_000_000_000.0:F1} GB";
        return $"{bytes / 1_000_000.0:F0} MB";
    }
}
