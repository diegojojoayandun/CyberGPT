using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(IChromaService chroma) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload([FromBody] DocumentUploadRequest req)
    {
        var id = Guid.NewGuid().ToString();
        var meta = new Dictionary<string, string>
        {
            ["fileName"]   = req.FileName,
            ["category"]   = req.Category,
            ["uploadedAt"] = DateTime.UtcNow.ToString("O")
        };
        await chroma.AddDocumentAsync(id, req.Content, meta);
        return Ok(new { id, message = "Document indexed." });
    }
}
