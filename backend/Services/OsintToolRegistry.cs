namespace CyberGPT.API.Services;

public class OsintToolRegistry(IEnumerable<IOsintTool> tools)
{
    private readonly Dictionary<string, IOsintTool> _tools = tools.ToDictionary(t => t.Name);

    public IOsintTool? Get(string name) => _tools.GetValueOrDefault(name);

    public List<object> ToOllamaSchema() =>
        _tools.Values.Select(t => (object)new
        {
            type     = "function",
            function = new { name = t.Name, description = t.Description, parameters = t.Parameters }
        }).ToList();
}
