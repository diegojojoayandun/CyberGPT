using CyberGPT.API.Services;

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

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
