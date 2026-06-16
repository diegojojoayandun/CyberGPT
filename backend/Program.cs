using CyberGPT.API.Data;
using CyberGPT.API.Services;
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

// Historial de conversación (SQLite)
builder.Services.AddDbContext<ChatHistoryContext>(o =>
    o.UseSqlite("Data Source=cybergpt.db"));
builder.Services.AddScoped<SessionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<ChatHistoryContext>().Database.EnsureCreated();

app.UseCors();
app.MapControllers();
app.Run();
