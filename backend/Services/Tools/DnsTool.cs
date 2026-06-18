using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// DNS lookup via Google DNS-over-HTTPS (free, no API key).
/// Supports A, AAAA, MX, NS, TXT, CNAME, SOA record types.
/// </summary>
public class DnsTool(IHttpClientFactory httpFactory) : IOsintTool
{
    private static readonly string[] DefaultTypes = ["A", "AAAA", "MX", "NS", "TXT", "CNAME"];

    public string Name        => "dns_lookup";
    public string Description => "Queries DNS records (A, AAAA, MX, NS, TXT, CNAME, SOA) for a domain. Returns IP addresses, mail servers, name servers and TXT records including SPF/DMARC/DKIM.";

    public object Parameters => new
    {
        type       = "object",
        properties = new
        {
            domain      = new { type = "string", description = "Domain name to query" },
            record_type = new
            {
                type        = "string",
                description = "DNS record type. Use 'ALL' to query A, AAAA, MX, NS, TXT and CNAME at once.",
                @enum       = new[] { "A", "AAAA", "MX", "NS", "TXT", "CNAME", "SOA", "ALL" }
            }
        },
        required   = new[] { "domain", "record_type" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var domain     = args.GetProperty("domain").GetString()?.Trim() ?? "";
        var recordType = args.GetProperty("record_type").GetString()?.ToUpperInvariant() ?? "A";

        if (string.IsNullOrEmpty(domain)) return "Error: domain is required.";

        var types = recordType == "ALL" ? DefaultTypes : [recordType];
        var http  = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(10);

        var sb = new StringBuilder();
        sb.AppendLine($"[DNS] {domain}");

        foreach (var type in types)
        {
            try
            {
                var url = $"https://dns.google/resolve?name={Uri.EscapeDataString(domain)}&type={type}";
                var raw = await http.GetStringAsync(url, ct);
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Answer", out var answers)) continue;

                var records = answers.EnumerateArray()
                    .Where(a => a.TryGetProperty("type", out var t) && DnsTypeMatches(t.GetInt32(), type))
                    .Select(a => a.TryGetProperty("data", out var d) ? d.GetString() : null)
                    .Where(d => d != null)
                    .Distinct()
                    .ToList();

                if (records.Count > 0)
                    sb.AppendLine($"{type,-6}: {string.Join(" | ", records)}");
            }
            catch { /* skip failed type */ }
        }

        return sb.Length > $"[DNS] {domain}\n".Length
            ? sb.ToString()
            : $"[DNS] {domain}\nNo records found.";
    }

    private static bool DnsTypeMatches(int typeNum, string typeName) => typeName switch
    {
        "A"     => typeNum == 1,
        "AAAA"  => typeNum == 28,
        "MX"    => typeNum == 15,
        "NS"    => typeNum == 2,
        "TXT"   => typeNum == 16,
        "CNAME" => typeNum == 5,
        "SOA"   => typeNum == 6,
        _       => true
    };
}
