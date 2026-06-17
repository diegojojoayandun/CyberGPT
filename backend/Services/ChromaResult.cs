namespace CyberGPT.API.Services;

public class ChromaResult
{
    public string DocId { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public float Distance { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
