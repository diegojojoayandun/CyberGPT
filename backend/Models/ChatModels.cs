namespace CyberGPT.API.Models;

public record ChatRequest(string Message, string? SessionId = null, string? Category = null, string? Model = null);
public record ChatResponse(string Reply, string SessionId, List<SourceChunk> Sources);
public record DocumentUploadRequest(string Content, string FileName, string Category);

public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime LastTimestamp { get; set; }
    public int MessageCount { get; set; }
}
