namespace CyberGPT.API.Models;

public record ChatRequest(string Message, string? SessionId = null);
public record ChatResponse(string Reply, string SessionId, List<string> Sources);
public record DocumentUploadRequest(string Content, string FileName, string Category);
