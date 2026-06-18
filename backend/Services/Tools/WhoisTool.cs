using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// WHOIS/RDAP lookup via rdap.org (free, no API key).
/// Works for both domains and IP addresses.
/// </summary>
public class WhoisTool(IHttpClientFactory httpFactory) : IOsintTool
{
    public string Name        => "whois_lookup";
    public string Description => "Performs WHOIS/RDAP lookup for a domain or IP to get registrar, registration dates, nameservers and status.";

    public object Parameters => new
    {
        type       = "object",
        properties = new { target = new { type = "string", description = "Domain name or IP address" } },
        required   = new[] { "target" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var target = args.GetProperty("target").GetString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(target)) return "Error: target is required.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(12);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/rdap+json");

        var isIp  = System.Net.IPAddress.TryParse(target, out _);
        var url   = isIp ? $"https://rdap.org/ip/{target}" : $"https://rdap.org/domain/{target}";

        try
        {
            var raw = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(raw);
            return FormatRdap(doc.RootElement, target);
        }
        catch (Exception ex)
        {
            return $"WHOIS lookup failed for '{target}': {ex.Message}";
        }
    }

    private static string FormatRdap(JsonElement root, string target)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[WHOIS] {target}");

        if (root.TryGetProperty("ldhName", out var ldhName))
            sb.AppendLine($"Name      : {ldhName.GetString()}");

        if (root.TryGetProperty("status", out var status))
            sb.AppendLine($"Status    : {string.Join(", ", status.EnumerateArray().Select(s => s.GetString()))}");

        if (root.TryGetProperty("events", out var events))
            foreach (var ev in events.EnumerateArray())
                if (ev.TryGetProperty("eventAction", out var action) && ev.TryGetProperty("eventDate", out var date))
                    sb.AppendLine($"{action.GetString(),-12}: {date.GetString()}");

        if (root.TryGetProperty("nameservers", out var ns))
        {
            var names = ns.EnumerateArray()
                .Select(n => n.TryGetProperty("ldhName", out var lh) ? lh.GetString() : null)
                .Where(n => n != null).ToList();
            if (names.Count > 0)
                sb.AppendLine($"Nameservers: {string.Join(", ", names)}");
        }

        if (root.TryGetProperty("entities", out var entities))
            foreach (var entity in entities.EnumerateArray())
            {
                var roles = entity.TryGetProperty("roles", out var r)
                    ? string.Join("/", r.EnumerateArray().Select(x => x.GetString()))
                    : "unknown";
                var fn = ExtractVcardFn(entity);
                if (fn != null) sb.AppendLine($"{roles,-12}: {fn}");
            }

        // For IPs: country + network info
        if (root.TryGetProperty("country", out var country))
            sb.AppendLine($"Country   : {country.GetString()}");
        if (root.TryGetProperty("name", out var name))
            sb.AppendLine($"Network   : {name.GetString()}");
        if (root.TryGetProperty("startAddress", out var start) && root.TryGetProperty("endAddress", out var end))
            sb.AppendLine($"Range     : {start.GetString()} - {end.GetString()}");

        return sb.ToString();
    }

    private static string? ExtractVcardFn(JsonElement entity)
    {
        if (!entity.TryGetProperty("vcardArray", out var vcard) || vcard.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var item in vcard.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array) continue;
            foreach (var field in item.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.Array) continue;
                var arr = field.EnumerateArray().ToList();
                if (arr.Count >= 4 && arr[0].GetString() == "fn")
                    return arr[3].ValueKind == JsonValueKind.String ? arr[3].GetString() : null;
            }
        }
        return null;
    }
}
