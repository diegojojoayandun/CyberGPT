using System.Text;
using System.Text.Json;
using CyberGPT.API.Models;

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

    public async Task<string> GenerateAsync(string prompt, string context = "", List<ChatTurn>? history = null)
    {
        var messages = new List<object> { new { role = "system", content = SystemPrompt } };

        if (history != null)
            foreach (var turn in history)
                messages.Add(new { role = turn.Role, content = turn.Message });

        var userContent = string.IsNullOrEmpty(context)
            ? prompt
            : $"Contexto RAG:\n{context}\n\nPregunta: {prompt}";

        messages.Add(new { role = "user", content = userContent });

        var body = new { model = _model, messages, stream = false };

        var res = await _http.PostAsync(
            $"{_baseUrl}/api/chat",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
