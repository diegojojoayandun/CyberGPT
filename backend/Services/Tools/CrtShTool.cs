using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// Subdomain discovery via crt.sh certificate transparency logs (free, no API key).
/// Finds all subdomains that appear in SSL/TLS certificate records.
/// </summary>
public class CrtShTool(IHttpClientFactory httpFactory) : IOsintTool
{
    public string Name        => "subdomain_discovery";
    public string Description => "Discovers subdomains of a domain by querying certificate transparency logs (crt.sh). Returns a list of unique subdomains found in SSL certificates.";

    public object Parameters => new
    {
        type       = "object",
        properties = new { domain = new { type = "string", description = "Base domain to search subdomains for (e.g. example.com)" } },
        required   = new[] { "domain" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var domain = args.GetProperty("domain").GetString()?.Trim().TrimStart('%', '.') ?? "";
        if (string.IsNullOrEmpty(domain)) return "Error: domain is required.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CyberGPT-OSINT/1.0");

        try
        {
            var url = $"https://crt.sh/?q=%.{domain}&output=json";
            var raw = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(raw);

            var subdomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                // crt.sh returns nameValue which can contain multiple names separated by newlines
                var nameValue = entry.TryGetProperty("name_value", out var nv) ? nv.GetString() : null;
                if (nameValue == null) continue;

                foreach (var name in nameValue.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var clean = name.TrimStart('*', '.');
                    if (clean.EndsWith(domain, StringComparison.OrdinalIgnoreCase) && !clean.Equals(domain, StringComparison.OrdinalIgnoreCase))
                        subdomains.Add(clean.ToLowerInvariant());
                }
            }

            if (subdomains.Count == 0)
                return $"[CertTransparency] {domain}\nNo subdomains found in certificate logs.";

            var sb = new StringBuilder();
            sb.AppendLine($"[CertTransparency] {domain} — {subdomains.Count} unique subdomains:");
            foreach (var sub in subdomains.OrderBy(s => s))
                sb.AppendLine($"  - {sub}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"crt.sh lookup failed for '{domain}': {ex.Message}";
        }
    }
}
