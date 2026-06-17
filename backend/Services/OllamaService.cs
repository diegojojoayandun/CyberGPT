using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CyberGPT.API.Models;

namespace CyberGPT.API.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    private static readonly string SystemPrompt = File.Exists("Prompts/cybergpt.txt")
        ? File.ReadAllText("Prompts/cybergpt.txt")
        : "Eres CyberGPT, especialista en ciberseguridad.";

    private static readonly string RewriteSystemPrompt =
        "You are a search query optimizer for a cybersecurity knowledge base. " +
        "Rewrite the user question as a concise, keyword-rich search query (max 20 words). " +
        "Output ONLY the rewritten query, nothing else.";

    public OllamaService(HttpClient http, IConfiguration config)
    {
        _baseUrl      = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = config["Ollama:Model"]   ?? "qwen3:1.7b";
        http.Timeout  = TimeSpan.FromMinutes(10);
        _http = http;
    }

    private string Resolve(string? model) => string.IsNullOrWhiteSpace(model) ? _defaultModel : model;

    // qwen3 supports "think":false to skip chain-of-thought
    private static bool SupportsThinkParam(string model) =>
        model.StartsWith("qwen3", StringComparison.OrdinalIgnoreCase);

    private static string StripThinkingTags(string text) =>
        Regex.Replace(text, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase).TrimStart('\n', '\r');

    private List<object> BuildMessages(string prompt, string context, List<ChatTurn>? history)
    {
        var messages = new List<object> { new { role = "system", content = SystemPrompt } };
        if (history != null)
            foreach (var turn in history)
                messages.Add(new { role = turn.Role, content = turn.Message });

        var userContent = string.IsNullOrEmpty(context)
            ? prompt
            : $"Contexto RAG:\n{context}\n\nPregunta: {prompt}";

        messages.Add(new { role = "user", content = userContent });
        return messages;
    }

    private async Task<string> PostChatAsync(List<object> messages, string resolvedModel)
    {
        var bodyDict = new Dictionary<string, object>
        {
            ["model"]    = resolvedModel,
            ["messages"] = messages,
            ["stream"]   = false
        };
        if (SupportsThinkParam(resolvedModel))
            bodyDict["think"] = false;

        var res = await _http.PostAsync(
            $"{_baseUrl}/api/chat",
            new StringContent(JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json"));

        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var raw = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        return StripThinkingTags(raw);
    }

    public Task<string> GenerateAsync(string prompt, string context = "", List<ChatTurn>? history = null, string? model = null)
        => PostChatAsync(BuildMessages(prompt, context, history), Resolve(model));

    public async Task<string> RewriteQueryAsync(string question, List<ChatTurn>? history = null, string? model = null)
    {
        var contextClue = history?.LastOrDefault(t => t.Role == "user")?.Message;
        var input = contextClue != null && question.Length < 60
            ? $"Previous question: {contextClue}\nNew question: {question}"
            : question;

        var messages = new List<object>
        {
            new { role = "system", content = RewriteSystemPrompt },
            new { role = "user",   content = input }
        };

        try
        {
            // Always use default (fast) model for query rewriting
            var rewritten = await PostChatAsync(messages, _defaultModel);
            return string.IsNullOrWhiteSpace(rewritten) ? question : rewritten.Trim();
        }
        catch
        {
            return question;
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt, string context = "", List<ChatTurn>? history = null,
        string? model = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var resolvedModel = Resolve(model);
        var messages = BuildMessages(prompt, context, history);

        var bodyDict = new Dictionary<string, object>
        {
            ["model"]    = resolvedModel,
            ["messages"] = messages,
            ["stream"]   = true
        };
        if (SupportsThinkParam(resolvedModel))
            bodyDict["think"] = false;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(bodyDict), Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var thinkBuf = new StringBuilder();
        bool inThink = false;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (root.TryGetProperty("done", out var done) && done.GetBoolean()) break;
            if (!root.TryGetProperty("message", out var msg) ||
                !msg.TryGetProperty("content", out var contentEl)) continue;

            var token = contentEl.GetString();
            if (string.IsNullOrEmpty(token)) continue;

            thinkBuf.Append(token);
            var buf = thinkBuf.ToString();

            if (!inThink && buf.Contains("<think>", StringComparison.OrdinalIgnoreCase))
            {
                inThink = true;
                var before = buf[..buf.IndexOf("<think>", StringComparison.OrdinalIgnoreCase)].TrimStart();
                thinkBuf.Clear();
                thinkBuf.Append(buf[(buf.IndexOf("<think>", StringComparison.OrdinalIgnoreCase) + 7)..]);
                if (!string.IsNullOrEmpty(before)) yield return before;
                continue;
            }

            if (inThink)
            {
                var endIdx = buf.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
                if (endIdx >= 0)
                {
                    inThink = false;
                    var after = buf[(endIdx + 8)..].TrimStart('\n');
                    thinkBuf.Clear();
                    if (!string.IsNullOrEmpty(after)) yield return after;
                }
                else if (thinkBuf.Length > 20000)
                    thinkBuf.Clear();
                continue;
            }

            var holdback = Math.Min(buf.Length, "<think>".Length - 1);
            var emitLen  = buf.Length - holdback;
            if (emitLen > 0)
            {
                yield return buf[..emitLen];
                thinkBuf.Remove(0, emitLen);
            }
        }

        if (!inThink && thinkBuf.Length > 0)
            yield return thinkBuf.ToString();
    }
}
