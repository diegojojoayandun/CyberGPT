using CyberGPT.API.Data;
using CyberGPT.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberGPT.API.Services;

public class SessionService(ChatHistoryContext db)
{
    public async Task AppendTurnAsync(string sessionId, string role, string message)
    {
        db.ChatTurns.Add(new ChatTurn { SessionId = sessionId, Role = role, Message = message, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public async Task<List<ChatTurn>> GetLastTurnsAsync(string sessionId, int n = 6) =>
        await db.ChatTurns
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.Timestamp)
            .Take(n)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

    public async Task<List<ChatTurn>> GetAllTurnsAsync(string sessionId) =>
        await db.ChatTurns
            .Where(t => t.SessionId == sessionId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

    public async Task<List<SessionInfo>> GetSessionsAsync(int limit = 30)
    {
        var sessionIds = await db.ChatTurns
            .GroupBy(t => t.SessionId)
            .Select(g => new { SessionId = g.Key, LastTimestamp = g.Max(t => t.Timestamp), Count = g.Count() })
            .OrderByDescending(g => g.LastTimestamp)
            .Take(limit)
            .ToListAsync();

        if (sessionIds.Count == 0) return [];

        var ids = sessionIds.Select(s => s.SessionId).ToList();
        var firstUserMessages = await db.ChatTurns
            .Where(t => ids.Contains(t.SessionId) && t.Role == "user")
            .GroupBy(t => t.SessionId)
            .Select(g => new { SessionId = g.Key, Message = g.OrderBy(t => t.Timestamp).First().Message })
            .ToListAsync();

        var titleMap = firstUserMessages.ToDictionary(m => m.SessionId, m => m.Message);

        return sessionIds.Select(s =>
        {
            var raw = titleMap.GetValueOrDefault(s.SessionId, "Sin título");
            return new SessionInfo
            {
                SessionId = s.SessionId,
                Title = raw.Length > 60 ? raw[..60] + "…" : raw,
                LastTimestamp = s.LastTimestamp,
                MessageCount = s.Count
            };
        }).ToList();
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var turns = db.ChatTurns.Where(t => t.SessionId == sessionId);
        db.ChatTurns.RemoveRange(turns);
        await db.SaveChangesAsync();
    }
}
