using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace CyberGPT.API.Services;

public class KeywordSearchService : IKeywordSearchService
{
    private readonly string _connectionString;

    public KeywordSearchService(IConfiguration config)
    {
        var dataSource = config.GetConnectionString("DefaultConnection") ?? "Data Source=cybergpt.db";
        _connectionString = dataSource;
    }

    public async Task InsertAsync(string docId, string content, string fileName, string category)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO chunks_fts(doc_id, content, file_name, category) VALUES (@id, @content, @fn, @cat)";
        cmd.Parameters.AddWithValue("@id", docId);
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@fn", fileName);
        cmd.Parameters.AddWithValue("@cat", category);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<KeywordResult>> SearchAsync(string query, int topK = 7, string? category = null)
    {
        var terms = BuildFts5Query(query);
        if (string.IsNullOrEmpty(terms)) return [];

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = category != null
            ? "SELECT doc_id, content, file_name, category, bm25(chunks_fts) as score FROM chunks_fts WHERE chunks_fts MATCH @q AND category = @cat ORDER BY score LIMIT @limit"
            : "SELECT doc_id, content, file_name, category, bm25(chunks_fts) as score FROM chunks_fts WHERE chunks_fts MATCH @q ORDER BY score LIMIT @limit";

        cmd.Parameters.AddWithValue("@q", terms);
        if (category != null) cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@limit", topK);

        var results = new List<KeywordResult>();
        try
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(new KeywordResult(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDouble(4)));
        }
        catch
        {
            // FTS5 table may not exist yet or query failed — return empty
        }

        return results;
    }

    // Build a safe FTS5 query: extract words, quote them, join with OR
    private static string BuildFts5Query(string raw)
    {
        var words = Regex.Split(raw, @"\W+")
            .Where(w => w.Length > 2)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(12)
            .Select(w => $"\"{w}\"")
            .ToList();

        return words.Count > 0 ? string.Join(" OR ", words) : string.Empty;
    }
}
