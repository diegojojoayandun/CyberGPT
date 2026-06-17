namespace CyberGPT.API.Services;

public record KeywordResult(string DocId, string Content, string FileName, string Category, double Score);

public interface IKeywordSearchService
{
    Task<List<KeywordResult>> SearchAsync(string query, int topK = 7, string? category = null);
}
