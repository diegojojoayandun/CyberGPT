using System.Text.Json;

namespace CyberGPT.API.Services;

public interface IOsintTool
{
    string Name        { get; }
    string Description { get; }
    object Parameters  { get; }
    Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default);
}
