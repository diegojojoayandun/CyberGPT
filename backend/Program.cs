using CyberGPT.API.Data;
using CyberGPT.API.Services;
using CyberGPT.API.Services.Tools;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Servicios IA — retry exponencial (3 intentos) + circuit breaker
builder.Services.AddHttpClient<IOllamaService, OllamaService>()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 2;
        o.Retry.Delay = TimeSpan.FromSeconds(2);
        o.TotalRequestTimeout.Timeout     = TimeSpan.FromMinutes(15);
        o.AttemptTimeout.Timeout          = TimeSpan.FromMinutes(5);
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(11); // >= 2x AttemptTimeout
    });
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 3;
        o.Retry.Delay = TimeSpan.FromSeconds(1);
    });

// ChromaDB y RAG
builder.Services.AddSingleton<IChromaService, ChromaService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<DocumentIngester>();

// OSINT Agent
builder.Services.AddTransient<IOsintTool, WhoisTool>();
builder.Services.AddTransient<IOsintTool, DnsTool>();
builder.Services.AddTransient<IOsintTool, CrtShTool>();
builder.Services.AddTransient<IOsintTool, GeoIpTool>();
builder.Services.AddTransient<IOsintTool, ShodanTool>();
builder.Services.AddTransient<IOsintTool, VirusTotalTool>();
builder.Services.AddTransient<IOsintTool, WhatsAppOsintTool>();
builder.Services.AddSingleton<OsintToolRegistry>();
builder.Services.AddScoped<OsintAgentService>();

// Búsqueda híbrida (SQLite FTS5)
builder.Services.AddSingleton<KeywordSearchService>();
builder.Services.AddSingleton<IKeywordSearchService>(sp => sp.GetRequiredService<KeywordSearchService>());

// Historial de conversación (SQLite)
const string dbPath = "Data Source=cybergpt.db";
builder.Services.AddDbContext<ChatHistoryContext>(o => o.UseSqlite(dbPath));
builder.Services.AddScoped<SessionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChatHistoryContext>().Database.EnsureCreated();

    // Crear tabla FTS5 para búsqueda híbrida (fuera del modelo EF)
    using var conn = new SqliteConnection(dbPath);
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts
        USING fts5(doc_id UNINDEXED, content, file_name UNINDEXED, category UNINDEXED)";
    cmd.ExecuteNonQuery();
}

app.UseCors();
app.MapControllers();
app.Run();
