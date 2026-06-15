namespace CyberGPT.API.Services;

public class RagService(IOllamaService ollama, IChromaService chroma) : IRagService
{
    public async Task<(string Reply, List<string> Sources)> AskAsync(string question)
    {
        var sources = await chroma.QueryAsync(question, topK: 3);
        var context  = string.Join("\n\n", sources);
        var reply    = await ollama.GenerateAsync(question, context);
        return (reply, sources);
    }
}
