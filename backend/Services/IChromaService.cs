namespace CyberGPT.API.Services;

public interface IChromaService
{
    Task AddDocumentAsync(string id, string content, Dictionary<string, string> metadata);
    Task<List<ChromaResult>> QueryAsync(string query, int topK = 3, string? category = null);
}
