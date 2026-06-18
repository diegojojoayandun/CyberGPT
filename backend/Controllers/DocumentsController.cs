using Microsoft.AspNetCore.Mvc;
using CyberGPT.API.Models;
using CyberGPT.API.Services;

namespace CyberGPT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(IChromaService chroma, DocumentIngester ingester) : ControllerBase
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

    [HttpPost("pdf")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<IActionResult> UploadPdf(IFormFile file, [FromForm] string category = "general")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".txt" && ext != ".md")
            return BadRequest(new { error = "Only PDF, TXT and MD files are supported." });

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");
        try
        {
            await using (var fs = System.IO.File.Create(tempPath))
                await file.CopyToAsync(fs);

            await ingester.IngestFileAsync(tempPath, file.FileName, category);
            return Ok(new { message = $"{file.FileName} indexado correctamente.", category });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }
}
