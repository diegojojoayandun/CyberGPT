using Microsoft.EntityFrameworkCore;
using CyberGPT.API.Models;

namespace CyberGPT.API.Data;

public class ChatHistoryContext : DbContext
{
    public ChatHistoryContext(DbContextOptions<ChatHistoryContext> options) : base(options) { }

    public DbSet<ChatTurn> ChatTurns { get; set; }
}
