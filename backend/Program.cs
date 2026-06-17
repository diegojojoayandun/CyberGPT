using CyberGPT.API.Data;
using CyberGPT.API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Servicios IA
builder.Services.AddHttpClient<IOllamaService, OllamaService>();
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();

// ChromaDB y RAG
builder.Services.AddSingleton<IChromaService, ChromaService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<DocumentIngester>();

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
