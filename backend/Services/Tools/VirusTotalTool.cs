using System.Net;
using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// VirusTotal v3 API — reputation and threat intelligence for domains, IPs, URLs and file hashes.
/// Free API key: 500 requests/day, 4 req/min. Set 'Osint:VirusTotalApiKey' in appsettings.json.
/// </summary>
public class VirusTotalTool(IHttpClientFactory httpFactory, IConfiguration config) : IOsintTool
{
    public string Name        => "virustotal_lookup";
    public string Description => "Checks a domain, IP, URL or file hash against 70+ antivirus engines and threat intel sources on VirusTotal. Returns detection stats, reputation and threat categories.";

    public object Parameters => new
    {
        type       = "object",
        properties = new
        {
            target = new { type = "string", description = "Domain, IP address, URL or file hash (MD5/SHA1/SHA256)" },
            target_type = new
            {
                type        = "string",
                description = "Type of the target",
                @enum       = new[] { "domain", "ip", "hash", "url" }
            }
        },
        required = new[] { "target", "target_type" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var target     = args.GetProperty("target").GetString()?.Trim() ?? "";
        var targetType = args.TryGetProperty("target_type", out var tt) ? tt.GetString() ?? "domain" : "domain";

        if (string.IsNullOrEmpty(target)) return "Error: target is required.";

        var apiKey = config["Osint:VirusTotalApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return "TOOL_UNAVAILABLE: VirusTotal requires an API key (Osint:VirusTotalApiKey in appsettings.json). " +
                   "Skip this tool and continue with other available tools.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.TryAddWithoutValidation("x-apikey", apiKey);

        try
        {
            var (endpoint, label) = targetType switch
            {
                "ip"   => ($"https://www.virustotal.com/api/v3/ip_addresses/{Uri.EscapeDataString(target)}", "ip"),
                "hash" => ($"https://www.virustotal.com/api/v3/files/{target}", "file"),
                "url"  => (BuildUrlEndpoint(target), "url"),
                _      => ($"https://www.virustotal.com/api/v3/domains/{Uri.EscapeDataString(target)}", "domain")
            };

            var raw = await http.GetStringAsync(endpoint, ct);
            using var doc = JsonDocument.Parse(raw);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                return "Unexpected VirusTotal response format.";

            return label == "file" ? FormatFile(data, target) : FormatHostOrIp(data, target, label);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return $"[VirusTotal] '{target}' not found in database (may be too new or not yet analyzed).";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return "VirusTotal API error: invalid key or quota exceeded.";
        }
        catch (Exception ex)
        {
            return $"VirusTotal lookup failed for '{target}': {ex.Message}";
        }
    }

    private static string BuildUrlEndpoint(string url)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(url))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"https://www.virustotal.com/api/v3/urls/{b64}";
    }

    private static string FormatHostOrIp(JsonElement data, string target, string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[VirusTotal] {label}: {target}");

        if (!data.TryGetProperty("attributes", out var attrs)) return sb + "No attributes found.";

        if (attrs.TryGetProperty("reputation", out var rep))
            sb.AppendLine($"Reputation : {rep.GetInt32()} (community score, negative = malicious)");

        if (attrs.TryGetProperty("last_analysis_stats", out var stats))
        {
            var malicious  = stats.TryGetProperty("malicious",  out var m) ? m.GetInt32() : 0;
            var suspicious = stats.TryGetProperty("suspicious", out var s) ? s.GetInt32() : 0;
            var harmless   = stats.TryGetProperty("harmless",   out var h) ? h.GetInt32() : 0;
            var undetected = stats.TryGetProperty("undetected", out var u) ? u.GetInt32() : 0;
            var total = malicious + suspicious + harmless + undetected;
            sb.AppendLine($"Detections : {malicious} malicious, {suspicious} suspicious / {total} engines");
            if (malicious + suspicious > 0)
                sb.AppendLine($"⚠️  FLAGGED by {malicious + suspicious} security vendors");
            else
                sb.AppendLine("✓  Clean — no detections");
        }

        if (attrs.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            var tagList = tags.EnumerateArray().Select(t => t.GetString()).Where(t => t != null).ToList();
            if (tagList.Count > 0) sb.AppendLine($"Tags       : {string.Join(", ", tagList)}");
        }

        if (attrs.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Object)
        {
            var catValues = cats.EnumerateObject()
                .Select(p => p.Value.GetString())
                .Where(v => v != null)
                .Distinct().Take(5).ToList();
            if (catValues.Count > 0) sb.AppendLine($"Categories : {string.Join(", ", catValues)}");
        }

        if (attrs.TryGetProperty("country", out var country)) sb.AppendLine($"Country    : {country.GetString()}");
        if (attrs.TryGetProperty("as_owner", out var asOwner)) sb.AppendLine($"AS Owner   : {asOwner.GetString()}");
        if (attrs.TryGetProperty("asn",      out var asn))     sb.AppendLine($"ASN        : {asn.GetRawText()}");

        // Top flagging vendors
        if (attrs.TryGetProperty("last_analysis_results", out var results) && results.ValueKind == JsonValueKind.Object)
        {
            var flagged = results.EnumerateObject()
                .Where(p => p.Value.TryGetProperty("result", out var r)
                            && r.GetString() is not null and not "clean" and not "unrated" and not "")
                .Take(6).ToList();

            if (flagged.Count > 0)
            {
                sb.AppendLine("Flagged by:");
                foreach (var f in flagged)
                {
                    var result = f.Value.TryGetProperty("result", out var r) ? r.GetString() : "?";
                    sb.AppendLine($"  • {f.Name}: {result}");
                }
            }
        }

        return sb.ToString();
    }

    private static string FormatFile(JsonElement data, string hash)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[VirusTotal] file hash: {hash}");

        if (!data.TryGetProperty("attributes", out var attrs)) return sb + "No attributes found.";

        if (attrs.TryGetProperty("meaningful_name", out var name)) sb.AppendLine($"Name       : {name.GetString()}");
        if (attrs.TryGetProperty("size",            out var size)) sb.AppendLine($"Size       : {size.GetInt64()} bytes");
        if (attrs.TryGetProperty("type_description",out var td))   sb.AppendLine($"Type       : {td.GetString()}");

        if (attrs.TryGetProperty("last_analysis_stats", out var stats))
        {
            var malicious  = stats.TryGetProperty("malicious",  out var m) ? m.GetInt32() : 0;
            var suspicious = stats.TryGetProperty("suspicious", out var s) ? s.GetInt32() : 0;
            var total = stats.EnumerateObject().Sum(p => p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetInt32() : 0);
            sb.AppendLine($"Detections : {malicious} malicious, {suspicious} suspicious / {total} engines");
            if (malicious > 0) sb.AppendLine($"⚠️  MALWARE DETECTED by {malicious} engines");
        }

        if (attrs.TryGetProperty("popular_threat_classification", out var threat)
            && threat.TryGetProperty("suggested_threat_label", out var tLabel))
            sb.AppendLine($"Threat     : {tLabel.GetString()}");

        if (attrs.TryGetProperty("names", out var names) && names.ValueKind == JsonValueKind.Array)
        {
            var nameList = names.EnumerateArray().Select(n => n.GetString()).Where(n => n != null).Take(5).ToList();
            if (nameList.Count > 0) sb.AppendLine($"Aliases    : {string.Join(", ", nameList)}");
        }

        // SHA256 for reference
        if (attrs.TryGetProperty("sha256", out var sha256)) sb.AppendLine($"SHA256     : {sha256.GetString()}");

        return sb.ToString();
    }
}
