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

    public async Task<List<ChromaResult>> QueryAsync(string query, int topK = 3, string? category = null)
    {
        var collectionId = await GetOrCreateCollectionAsync();
        var embedding = await _embedding.GetEmbeddingAsync(query);

        var bodyDict = new Dictionary<string, object>
        {
            ["query_embeddings"] = new[] { embedding },
            ["n_results"] = topK,
            ["include"] = new[] { "documents", "metadatas", "distances" }
        };

        if (category != null)
            bodyDict["where"] = new Dictionary<string, object>
            {
                ["category"] = new Dictionary<string, string> { ["$eq"] = category }
            };

        var res = await _http.PostAsync(
            V2($"collections/{collectionId}/query"),
            new StringContent(JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode) return [];

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var docsEl  = root.GetProperty("documents")[0];
        var idsEl   = root.TryGetProperty("ids",       out var idsOuter)  ? idsOuter[0]   : default;
        var distsEl = root.TryGetProperty("distances", out var distsOuter) ? distsOuter[0] : default;
        var metasEl = root.TryGetProperty("metadatas", out var metasOuter) ? metasOuter[0] : default;

        var idsList   = idsEl.ValueKind   == JsonValueKind.Array ? idsEl.EnumerateArray().Select(x => x.GetString() ?? "").ToList()   : null;
        var distList  = distsEl.ValueKind == JsonValueKind.Array ? distsEl.EnumerateArray().Select(x => x.GetSingle()).ToList()       : null;

        var results = new List<ChromaResult>();
        int idx = 0;
        foreach (var d in docsEl.EnumerateArray())
        {
            var meta = new Dictionary<string, string>();
            if (metasEl.ValueKind == JsonValueKind.Array)
            {
                var metaEl = metasEl.EnumerateArray().ElementAtOrDefault(idx);
                if (metaEl.ValueKind == JsonValueKind.Object)
                    foreach (var prop in metaEl.EnumerateObject())
                        meta[prop.Name] = prop.Value.GetString() ?? "";
            }

            results.Add(new ChromaResult
            {
                DocId    = idsList  != null && idx < idsList.Count  ? idsList[idx]  : "",
                Document = d.GetString() ?? "",
                Distance = distList != null && idx < distList.Count ? distList[idx] : 0f,
                Metadata = meta
            });
            idx++;
        }

        return results;
    }
}
