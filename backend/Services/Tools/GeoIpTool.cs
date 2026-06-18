using System.Text;
using System.Text.Json;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// IP geolocation and ASN lookup via ip-api.com (free for non-commercial use, no API key).
/// Also resolves hostnames to IPs before lookup.
/// </summary>
public class GeoIpTool(IHttpClientFactory httpFactory) : IOsintTool
{
    public string Name        => "geoip_lookup";
    public string Description => "Returns geolocation, ISP, organization and ASN information for an IP address or domain. Useful for identifying hosting providers and geographic location.";

    public object Parameters => new
    {
        type       = "object",
        properties = new { target = new { type = "string", description = "IP address or domain name to geolocate" } },
        required   = new[] { "target" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var target = args.GetProperty("target").GetString()?.Trim() ?? "";
        if (string.IsNullOrEmpty(target)) return "Error: target is required.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var fields = "status,message,country,countryCode,regionName,city,zip,lat,lon,timezone,isp,org,as,query,reverse";
            var url    = $"http://ip-api.com/json/{Uri.EscapeDataString(target)}?fields={fields}";
            var raw    = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var status) && status.GetString() == "fail")
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                return $"GeoIP lookup failed for '{target}': {msg}";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[GeoIP] {target}");

            void Add(string label, string key)
            {
                if (root.TryGetProperty(key, out var val) && val.ValueKind != JsonValueKind.Null)
                {
                    var str = val.ValueKind == JsonValueKind.String ? val.GetString() : val.GetRawText();
                    if (!string.IsNullOrWhiteSpace(str))
                        sb.AppendLine($"{label,-12}: {str}");
                }
            }

            Add("Resolved IP",  "query");
            Add("Reverse DNS",  "reverse");
            Add("Country",      "country");
            Add("Country Code", "countryCode");
            Add("Region",       "regionName");
            Add("City",         "city");
            Add("Timezone",     "timezone");
            Add("ISP",          "isp");
            Add("Organization", "org");
            Add("ASN",          "as");

            if (root.TryGetProperty("lat", out var lat) && root.TryGetProperty("lon", out var lon))
                sb.AppendLine($"Coordinates: {lat.GetDouble():F4}, {lon.GetDouble():F4}");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"GeoIP lookup failed for '{target}': {ex.Message}";
        }
    }
}
