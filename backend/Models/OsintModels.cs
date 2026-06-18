using System.Text.Json;

namespace CyberGPT.API.Models;

public record OsintRequest(string Target, string TargetType = "auto");

public record LlmToolCall(string Name, JsonElement Arguments);
