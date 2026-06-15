using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController(DocumentIngester ingester, IConfiguration config) : ControllerBase
{
    [HttpPost("folder")]
    public async Task<IActionResult> IngestFolder()
    {
        var knowledgePath = config["Knowledge:Path"] ?? "knowledge";
        await ingester.IngestFolderAsync(knowledgePath);
        return Ok(new { message = "Indexación completada.", path = knowledgePath });
    }

    [HttpPost("file")]
    public async Task<IActionResult> IngestFile([FromBody] string filePath)
    {
        await ingester.IngestFileAsync(filePath);
        return Ok(new { message = $"Archivo indexado: {filePath}" });
    }
}
