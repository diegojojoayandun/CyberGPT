using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services;

public class ChromaService : IChromaService
{
    private readonly HttpClient _http;
    private readonly IEmbeddingService _embedding;
    private readonly string _baseUrl;
    private const string CollectionName = "cybergpt";
    private const string Tenant = "default_tenant";
    private const string Database = "default_database";
    private string? _collectionId;

    public ChromaService(IConfiguration config, IEmbeddingService embedding)
    {
        _http = new HttpClient();
        _embedding = embedding;
        _baseUrl = config["Chroma:BaseUrl"] ?? "http://localhost:8000";
    }

    private string V2(string path) =>
        $"{_baseUrl}/api/v2/tenants/{Tenant}/databases/{Database}/{path}";

    private async Task<string> GetOrCreateCollectionAsync()
    {
        if (_collectionId != null) return _collectionId;

        // Buscar colección existente
        var listRes = await _http.GetAsync(V2("collections"));
        if (listRes.IsSuccessStatusCode)
        {
            var json = await listRes.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

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

        // Crear colección nueva
        var body = new
        {
            name = CollectionName,
            metadata = new Dictionary<string, string> { ["hnsw:space"] = "cosine" }
        };

        var createRes = await _http.PostAsync(
            V2("collections"),
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
            V2($"collections/{collectionId}/upsert"),
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
    }

    public async Task<List<ChromaResult>> QueryAsync(string query, int topK = 3)
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
            V2($"collections/{collectionId}/query"),
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode) return new List<ChromaResult>();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

        var documents = doc.RootElement.GetProperty("documents")[0];
        var distances = doc.RootElement.TryGetProperty("distances", out var distsArr)
            ? distsArr[0]
            : default;

        var results = new List<ChromaResult>();
        var docEnum = documents.EnumerateArray();
        var distList = distances.ValueKind == JsonValueKind.Array
            ? distances.EnumerateArray().Select(x => x.GetSingle()).ToList()
            : null;

        int idx = 0;
        foreach (var d in docEnum)
        {
            var text = d.GetString() ?? string.Empty;
            var distance = distList != null && idx < distList.Count ? distList[idx] : 0f;
            results.Add(new ChromaResult { Document = text, Distance = distance });
            idx++;
        }

        return results;
    }
}
