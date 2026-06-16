using System;

namespace CyberGPT.API.Models;

public class ChatTurn
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Message { get; set; } = string.Empty;
}
