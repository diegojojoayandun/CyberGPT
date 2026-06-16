using CyberGPT.API.Data;
using CyberGPT.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberGPT.API.Services;

public class SessionService
{
    private readonly ChatHistoryContext _db;

    public SessionService(ChatHistoryContext db)
    {
        _db = db;
    }

    public async Task AppendTurnAsync(string sessionId, string role, string message)
    {
        var turn = new ChatTurn { SessionId = sessionId, Role = role, Message = message, Timestamp = DateTime.UtcNow };
        _db.ChatTurns.Add(turn);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ChatTurn>> GetLastTurnsAsync(string sessionId, int n = 6)
    {
        return await _db.ChatTurns
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.Timestamp)
            .Take(n)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }
}
