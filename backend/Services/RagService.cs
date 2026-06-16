using System.Linq;
using CyberGPT.API.Models;

namespace CyberGPT.API.Services;

public class RagService(IOllamaService ollama, IChromaService chroma) : IRagService
{
    public async Task<(string Reply, List<string> Sources)> AskAsync(string question, List<ChatTurn>? history = null)
    {
        // Recuperar más resultados y re-rankear localmente por distancia devuelta por Chroma
        var raw = await chroma.QueryAsync(question, topK: 7);

        // Orden ascendente por distancia (más cercano primero) y seleccionar top 5 finales
        var ranked = raw
            .OrderBy(r => r.Distance)
            .Take(5)
            .Select(r => r.Document)
            .ToList();

        var context = string.Join("\n\n", ranked);
        var reply = await ollama.GenerateAsync(question, context, history);
        return (reply, ranked);
    }
}
