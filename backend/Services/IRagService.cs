using CyberGPT.API.Models;

namespace CyberGPT.API.Services;
public interface IRagService
{
    Task<(string Reply, List<SourceChunk> Sources)> AskAsync(string question, List<ChatTurn>? history = null, string? category = null, string? model = null);
    Task<(List<SourceChunk> Sources, string Context)> RetrieveAsync(string question, List<ChatTurn>? history = null, int topK = 5, string? category = null, string? model = null);
}
