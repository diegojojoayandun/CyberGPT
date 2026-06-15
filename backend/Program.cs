using CyberGPT.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHttpClient<IOllamaService, OllamaService>();
builder.Services.AddSingleton<IChromaService, ChromaService>();
builder.Services.AddScoped<IRagService, RagService>();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
