using CyberGPT.API.Models;

namespace CyberGPT.API.Services;

public class RagService(IOllamaService ollama, IChromaService chroma, IKeywordSearchService keyword) : IRagService
{
    private const double RrfK = 60.0;

    public async Task<(List<SourceChunk> Sources, string Context)> RetrieveAsync(
        string question, List<ChatTurn>? history = null, int topK = 5, string? category = null, string? model = null)
    {
        // 1. Rewrite query for better retrieval (always uses default/fast model)
        var searchQuery = await ollama.RewriteQueryAsync(question, history);

        // 2. Parallel: semantic search (Chroma) + keyword search (SQLite FTS5)
        var (semanticTask, keywordTask) = (
            chroma.QueryAsync(searchQuery, topK: topK * 2, category: category),
            keyword.SearchAsync(searchQuery, topK: topK * 2, category: category)
        );
        await Task.WhenAll(semanticTask, keywordTask);

        var semantic = semanticTask.Result;
        var kw = keywordTask.Result;

        // 3. Reciprocal Rank Fusion
        var fused = ApplyRRF(semantic, kw, topK);

        return (fused, string.Join("\n\n", fused.Select(s => s.Content)));
    }

    public async Task<(string Reply, List<SourceChunk> Sources)> AskAsync(
        string question, List<ChatTurn>? history = null, string? category = null, string? model = null)
    {
        var (sources, context) = await RetrieveAsync(question, history, topK: 5, category: category, model: model);
        var reply = await ollama.GenerateAsync(question, context, history, model);
        return (reply, sources);
    }

    private static List<SourceChunk> ApplyRRF(List<ChromaResult> semantic, List<KeywordResult> kw, int topK)
    {
        var scores = new Dictionary<string, (double Score, SourceChunk Chunk)>();

        for (int i = 0; i < semantic.Count; i++)
        {
            var r = semantic[i];
            var id = string.IsNullOrEmpty(r.DocId) ? $"sem_{i}" : r.DocId;
            var score = 1.0 / (RrfK + i + 1);
            var chunk = new SourceChunk
            {
                Content  = r.Document,
                FileName = r.Metadata.GetValueOrDefault("fileName", ""),
                Category = r.Metadata.GetValueOrDefault("category", "")
            };
            scores[id] = scores.TryGetValue(id, out var ex)
                ? (ex.Score + score, ex.Chunk)
                : (score, chunk);
        }

        for (int i = 0; i < kw.Count; i++)
        {
            var r = kw[i];
            var score = 1.0 / (RrfK + i + 1);
            var chunk = new SourceChunk { Content = r.Content, FileName = r.FileName, Category = r.Category };
            scores[r.DocId] = scores.TryGetValue(r.DocId, out var ex)
                ? (ex.Score + score, ex.Chunk)
                : (score, chunk);
        }

        return scores.Values
            .OrderByDescending(v => v.Score)
            .Take(topK)
            .Select(v => v.Chunk)
            .ToList();
    }
}
