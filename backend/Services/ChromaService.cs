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

    // Obtiene o crea la colección en ChromaDB
    private async Task<string> GetOrCreateCollectionAsync()
    {
        if (_collectionId != null) return _collectionId;

        // Intentar obtener colección existente
        var getRes = await _http.GetAsync($"{_baseUrl}/api/v1/collections/{CollectionName}");
        if (getRes.IsSuccessStatusCode)
        {
            using var getDoc = JsonDocument.Parse(await getRes.Content.ReadAsStringAsync());
            _collectionId = getDoc.RootElement.GetProperty("id").GetString()!;
            return _collectionId;
        }

        // Crear colección nueva con metadata de distancia coseno
        var body = new
        {
            name = CollectionName,
            metadata = new { @__hnsw_space = "cosine" }
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

        // ChromaDB devuelve documents como array de arrays
        var documents = doc.RootElement
            .GetProperty("documents")[0]
            .EnumerateArray()
            .Select(d => d.GetString() ?? "")
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList();

        return documents;
    }
}
