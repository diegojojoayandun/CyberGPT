using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services;

public class EmbeddingService(HttpClient http, IConfiguration config) : IEmbeddingService
{
    private readonly string _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    private const string EmbedModel = "nomic-embed-text";

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var body = new { model = EmbedModel, prompt = text };
        var res = await http.PostAsync(
            $"{_baseUrl}/api/embeddings",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }
}
