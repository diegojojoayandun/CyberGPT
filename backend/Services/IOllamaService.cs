using System.Text.Json;
using CyberGPT.API.Models;

namespace CyberGPT.API.Services;
public interface IOllamaService
{
    Task<string> GenerateAsync(string prompt, string context = "", List<ChatTurn>? history = null, string? model = null);
    Task<string> RewriteQueryAsync(string question, List<ChatTurn>? history = null, string? model = null);
    IAsyncEnumerable<string> StreamAsync(string prompt, string context = "", List<ChatTurn>? history = null, string? model = null, bool? enableThinking = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a chat request with tool definitions and returns content + any tool calls the model wants to make.
    /// </summary>
    Task<(string Content, List<LlmToolCall> ToolCalls)> ChatWithToolsAsync(
        List<object> messages,
        List<object> tools,
        string? model = null,
        CancellationToken ct = default);
}
