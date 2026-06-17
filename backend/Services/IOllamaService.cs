using CyberGPT.API.Models;

namespace CyberGPT.API.Services;
public interface IOllamaService
{
    Task<string> GenerateAsync(string prompt, string context = "", List<ChatTurn>? history = null, string? model = null);
    Task<string> RewriteQueryAsync(string question, List<ChatTurn>? history = null, string? model = null);
    IAsyncEnumerable<string> StreamAsync(string prompt, string context = "", List<ChatTurn>? history = null, string? model = null, CancellationToken ct = default);
}
