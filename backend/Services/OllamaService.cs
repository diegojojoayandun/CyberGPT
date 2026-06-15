using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    private static readonly string SystemPrompt = File.Exists("Prompts/cybergpt.txt")
        ? File.ReadAllText("Prompts/cybergpt.txt")
        : "Eres CyberGPT, especialista en ciberseguridad.";

    public OllamaService(IConfiguration config)
    {
        _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model   = config["Ollama:Model"]   ?? "qwen3:4b";

        // Timeout de 10 minutos — el modelo puede tardar en CPU
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task<string> GenerateAsync(string prompt, string context = "")
    {
        var fullPrompt = string.IsNullOrEmpty(context)
            ? prompt
            : $"Contexto RAG:\n{context}\n\nPregunta: {prompt}";

        var body = new
        {
            model  = _model,
            prompt = fullPrompt,
            system = SystemPrompt,
            stream = false
        };

        var res = await _http.PostAsync(
            $"{_baseUrl}/api/generate",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }
}
