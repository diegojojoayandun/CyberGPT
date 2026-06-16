using CyberGPT.API.Models;

namespace CyberGPT.API.Services;
public interface IRagService
{
    Task<(string Reply, List<string> Sources)> AskAsync(string question, List<ChatTurn>? history = null);
}
