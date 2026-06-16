using UglyToad.PdfPig;

namespace CyberGPT.API.Services;

public class DocumentIngester(IChromaService chroma, ILogger<DocumentIngester> logger)
{
    private static readonly string[] SupportedExtensions = [".txt", ".md", ".cs", ".json", ".yaml", ".yml", ".pdf"];
    private const int ChunkSize = 1000;   // caracteres por chunk
    private const int ChunkOverlap = 200; // solapamiento entre chunks

    public async Task IngestFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning("Carpeta no encontrada: {Path}", folderPath);
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        logger.LogInformation("Indexando {Count} archivos de {Path}", files.Count, folderPath);

        foreach (var file in files)
        {
            await IngestFileAsync(file);
        }

        logger.LogInformation("Indexación completada.");
    }

    public async Task IngestFileAsync(string filePath)
    {
        try
        {
            var content = await ReadContentAsync(filePath);
            var category = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? "general";
            var fileName = Path.GetFileName(filePath);

            var chunks = ChunkText(content);
            logger.LogInformation("Indexando {File} → {Chunks} chunks", fileName, chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                var id = $"{fileName}_{i}";
                var metadata = new Dictionary<string, string>
                {
                    ["fileName"] = fileName,
                    ["category"] = category,
                    ["chunkIndex"] = i.ToString(),
                    ["totalChunks"] = chunks.Count.ToString(),
                    ["filePath"] = filePath
                };

                await chroma.AddDocumentAsync(id, chunks[i], metadata);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error indexando {File}", filePath);
        }
    }

    private static Task<string> ReadContentAsync(string filePath)
    {
        if (Path.GetExtension(filePath).ToLower() == ".pdf")
        {
            using var pdf = PdfDocument.Open(filePath);
            var pages = pdf.GetPages()
                .Select(p => string.Join(" ", p.GetWords().Select(w => w.Text)));
            return Task.FromResult(string.Join("\n\n", pages));
        }
        return File.ReadAllTextAsync(filePath);
    }

    private static List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;

        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + ChunkSize, text.Length);
            chunks.Add(text[start..end]);
            start += ChunkSize - ChunkOverlap;
        }
        return chunks;
    }
}
