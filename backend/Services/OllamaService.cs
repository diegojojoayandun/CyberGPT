using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services;

public class OllamaService(HttpClient http, IConfiguration config) : IOllamaService
{
    private readonly string _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    private readonly string _model   = config["Ollama:Model"]   ?? "qwen3:4b";

    private static readonly string SystemPrompt = File.Exists("Prompts/cybergpt.txt")
        ? File.ReadAllText("Prompts/cybergpt.txt")
        : "Eres CyberGPT, especialista en ciberseguridad.";

    public async Task<string> GenerateAsync(string prompt, string context = "")
    {
        var fullPrompt = string.IsNullOrEmpty(context)
            ? prompt
            : $"Contexto RAG:\n{context}\n\nPregunta: {prompt}";

        var body = new { model = _model, prompt = fullPrompt, system = SystemPrompt, stream = false };

        var res = await http.PostAsync(
            $"{_baseUrl}/api/generate",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }
}
