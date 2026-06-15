namespace CyberGPT.API.Services;

public class ChromaService(IConfiguration config) : IChromaService
{
    public Task AddDocumentAsync(string id, string content, Dictionary<string, string> metadata)
        => Task.CompletedTask; // TODO: ChromaDB upsert

    public Task<List<string>> QueryAsync(string query, int topK = 3)
        => Task.FromResult(new List<string>()); // TODO: ChromaDB semantic search
}
