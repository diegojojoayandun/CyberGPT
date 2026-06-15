using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services;

public class ChromaService : IChromaService
{
    private readonly HttpClient _http;
    private readonly IEmbeddingService _embedding;
    private readonly string _baseUrl;
    private const string CollectionName = "cybergpt";
    private string? _collectionId;

    public ChromaService(IConfiguration config, IEmbeddingService embedding)
    {
        _http = new HttpClient();
        _embedding = embedding;
        _baseUrl = config["Chroma:BaseUrl"] ?? "http://localhost:8000";
    }

    private async Task<string> GetOrCreateCollectionAsync()
    {
        if (_collectionId != null) return _collectionId;

        // ChromaDB 1.x: listar colecciones y buscar por nombre
        var listRes = await _http.GetAsync($"{_baseUrl}/api/v1/collections");
        if (listRes.IsSuccessStatusCode)
        {
            var listJson = await listRes.Content.ReadAsStringAsync();
            using var listDoc = JsonDocument.Parse(listJson);
            var root = listDoc.RootElement;

            var collections = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("collections", out var c) ? c : default;

            if (collections.ValueKind == JsonValueKind.Array)
            {
                foreach (var col in collections.EnumerateArray())
                {
                    if (col.TryGetProperty("name", out var nameEl) &&
                        nameEl.GetString() == CollectionName)
                    {
                        _collectionId = col.GetProperty("id").GetString()!;
                        return _collectionId;
                    }
                }
            }
        }

        // No existe — crear colección nueva
        var body = new
        {
            name = CollectionName,
            metadata = new Dictionary<string, string> { ["hnsw:space"] = "cosine" }
        };
        var createRes = await _http.PostAsync(
            $"{_baseUrl}/api/v1/collections",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        createRes.EnsureSuccessStatusCode();
        using var createDoc = JsonDocument.Parse(await createRes.Content.ReadAsStringAsync());
        _collectionId = createDoc.RootElement.GetProperty("id").GetString()!;
        return _collectionId;
    }

    public async Task AddDocumentAsync(string id, string content, Dictionary<string, string> metadata)
    {
        var collectionId = await GetOrCreateCollectionAsync();
        var embedding = await _embedding.GetEmbeddingAsync(content);

        var body = new
        {
            ids = new[] { id },
            embeddings = new[] { embedding },
            documents = new[] { content },
            metadatas = new[] { metadata }
        };

        var res = await _http.PostAsync(
            $"{_baseUrl}/api/v1/collections/{collectionId}/upsert",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> QueryAsync(string query, int topK = 3)
    {
        var collectionId = await GetOrCreateCollectionAsync();
        var embedding = await _embedding.GetEmbeddingAsync(query);

        var body = new
        {
            query_embeddings = new[] { embedding },
            n_results = topK,
            include = new[] { "documents", "metadatas", "distances" }
        };

        var res = await _http.PostAsync(
            $"{_baseUrl}/api/v1/collections/{collectionId}/query",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode) return new List<string>();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

        return doc.RootElement
            .GetProperty("documents")[0]
            .EnumerateArray()
            .Select(d => d.GetString() ?? "")
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList();
    }
}
