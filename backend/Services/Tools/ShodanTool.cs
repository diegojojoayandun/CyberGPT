using System.Net;
using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// Shodan host lookup. Uses InternetDB (free, no key) or full Shodan API if key is configured.
/// </summary>
public class ShodanTool(IHttpClientFactory httpFactory, IConfiguration config) : IOsintTool
{
    public string Name        => "shodan_lookup";
    public string Description => "Queries Shodan for open ports, running services, CVEs and banners on an IP address. Reveals what is exposed to the internet and known vulnerabilities.";

    public object Parameters => new
    {
        type       = "object",
        properties = new { target = new { type = "string", description = "IP address or domain name to look up in Shodan" } },
        required   = new[] { "target" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var target = args.GetProperty("target").GetString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(target)) return "Error: target is required.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(15);

        // Resolve domain → IP
        var ip = target;
        if (!IPAddress.TryParse(target, out _))
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(target, ct);
                var v4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (v4 == null) return $"Could not resolve '{target}' to an IPv4 address.";
                ip = v4.ToString();
            }
            catch (Exception ex)
            {
                return $"DNS resolution failed for '{target}': {ex.Message}";
            }
        }

        var apiKey = config["Osint:ShodanApiKey"];
        return string.IsNullOrWhiteSpace(apiKey)
            ? await QueryInternetDb(http, ip, target, ct)
            : await QueryShodanApi(http, ip, target, apiKey, ct);
    }

    private static async Task<string> QueryInternetDb(HttpClient http, string ip, string original, CancellationToken ct)
    {
        try
        {
            var raw = await http.GetStringAsync($"https://internetdb.shodan.io/{ip}", ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var sb = new StringBuilder();
            sb.AppendLine($"[Shodan InternetDB] {original} → {ip}");

            if (root.TryGetProperty("ports", out var ports) && ports.ValueKind == JsonValueKind.Array)
            {
                var portList = ports.EnumerateArray().Select(p => p.GetRawText()).ToList();
                if (portList.Count > 0)
                    sb.AppendLine($"Open ports : {string.Join(", ", portList)}");
            }

            if (root.TryGetProperty("hostnames", out var hostnames) && hostnames.ValueKind == JsonValueKind.Array)
            {
                var names = hostnames.EnumerateArray().Select(h => h.GetString()).Where(h => h != null).ToList();
                if (names.Count > 0)
                    sb.AppendLine($"Hostnames  : {string.Join(", ", names)}");
            }

            if (root.TryGetProperty("cpes", out var cpes) && cpes.ValueKind == JsonValueKind.Array)
            {
                var cpeList = cpes.EnumerateArray().Select(c => c.GetString()).Where(c => c != null).ToList();
                if (cpeList.Count > 0)
                    sb.AppendLine($"CPEs       : {string.Join(", ", cpeList)}");
            }

            if (root.TryGetProperty("vulns", out var vulns) && vulns.ValueKind == JsonValueKind.Array)
            {
                var vulnList = vulns.EnumerateArray().Select(v => v.GetString()).Where(v => v != null).ToList();
                if (vulnList.Count > 0)
                {
                    var shown = vulnList.Take(10).ToList();
                    var extra = vulnList.Count > 10 ? $" … +{vulnList.Count - 10} more" : "";
                    sb.AppendLine($"CVEs ({vulnList.Count})  : {string.Join(", ", shown)}{extra}");
                    sb.AppendLine("⚠️  Known vulnerabilities found!");
                }
            }

            if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                var tagList = tags.EnumerateArray().Select(t => t.GetString()).Where(t => t != null).ToList();
                if (tagList.Count > 0)
                    sb.AppendLine($"Tags       : {string.Join(", ", tagList)}");
            }

            return sb.ToString();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return $"[Shodan InternetDB] {original} → {ip}\nNo data found (host not indexed).";
        }
        catch (Exception ex)
        {
            return $"Shodan InternetDB lookup failed for '{original}': {ex.Message}";
        }
    }

    private static async Task<string> QueryShodanApi(HttpClient http, string ip, string original, string apiKey, CancellationToken ct)
    {
        try
        {
            var raw = await http.GetStringAsync($"https://api.shodan.io/shodan/host/{ip}?key={apiKey}", ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var sb = new StringBuilder();
            sb.AppendLine($"[Shodan] {original} → {ip}");

            void Add(string label, string key)
            {
                if (root.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null)
                    sb.AppendLine($"{label,-12}: {(v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText())}");
            }

            Add("Org",     "org");
            Add("ISP",     "isp");
            Add("Country", "country_name");
            Add("City",    "city");
            Add("OS",      "os");

            if (root.TryGetProperty("ports", out var ports) && ports.ValueKind == JsonValueKind.Array)
                sb.AppendLine($"Open ports : {string.Join(", ", ports.EnumerateArray().Select(p => p.GetRawText()))}");

            if (root.TryGetProperty("vulns", out var vulns) && vulns.ValueKind == JsonValueKind.Object)
            {
                var vulnList = vulns.EnumerateObject().Select(p => p.Name).ToList();
                if (vulnList.Count > 0)
                {
                    var shown = vulnList.Take(10).ToList();
                    var extra = vulnList.Count > 10 ? $" … +{vulnList.Count - 10} more" : "";
                    sb.AppendLine($"CVEs ({vulnList.Count})  : {string.Join(", ", shown)}{extra}");
                    sb.AppendLine("⚠️  Known vulnerabilities found!");
                }
            }

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("Services:");
                foreach (var svc in data.EnumerateArray().Take(8))
                {
                    var port      = svc.TryGetProperty("port",      out var p)  ? p.GetRawText()   : "?";
                    var transport = svc.TryGetProperty("transport", out var tr) ? tr.GetString()   : "tcp";
                    var product   = svc.TryGetProperty("product",   out var pr) ? pr.GetString()   : null;
                    var version   = svc.TryGetProperty("version",   out var vr) ? vr.GetString()   : null;
                    var line = $"  • {port}/{transport}";
                    if (product != null) line += $"  {product}";
                    if (version != null) line += $" {version}";
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Shodan API lookup failed for '{original}': {ex.Message}";
        }
    }
}
