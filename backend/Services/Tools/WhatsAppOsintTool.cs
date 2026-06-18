using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CyberGPT.API.Services.Tools;

/// <summary>
/// WhatsApp OSINT via RapidAPI (whatsapp-osint.p.rapidapi.com).
/// Retrieves status, business info, linked devices and privacy settings for a phone number.
/// Set 'Osint:RapidApiKey' in appsettings.json (free plan at rapidapi.com).
/// </summary>
public class WhatsAppOsintTool(IHttpClientFactory httpFactory, IConfiguration config) : IOsintTool
{
    private const string Host    = "whatsapp-osint.p.rapidapi.com";
    private const string BaseUrl = "https://whatsapp-osint.p.rapidapi.com";

    public string Name        => "whatsapp_osint";
    public string Description => "Performs WhatsApp OSINT on a phone number: retrieves user status/about, business account verification, linked devices and privacy settings.";

    public object Parameters => new
    {
        type       = "object",
        properties = new
        {
            phone = new
            {
                type        = "string",
                description = "Phone number with country code, digits only. Examples: 5491112345678 (Argentina), 12025551234 (USA)"
            }
        },
        required = new[] { "phone" }
    };

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var raw = args.GetProperty("phone").GetString()?.Trim() ?? "";
        var phone = Regex.Replace(raw, @"[^\d]", "");

        if (phone.Length < 8 || phone.Length > 15)
            return $"Invalid phone number '{raw}'. Provide digits only with country code (8-15 digits).";

        var apiKey = config["Osint:RapidApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return "TOOL_UNAVAILABLE: WhatsApp OSINT requires a RapidAPI key (Osint:RapidApiKey in appsettings.json). " +
                   "Skip this tool and continue with other available tools.";

        var http = httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.TryAddWithoutValidation("x-rapidapi-key",  apiKey);
        http.DefaultRequestHeaders.TryAddWithoutValidation("x-rapidapi-host", Host);

        var sb = new StringBuilder();
        sb.AppendLine($"[WhatsApp OSINT] +{phone}");

        // Run all endpoints in parallel
        var tasks = new[]
        {
            FetchAsync(http, $"{BaseUrl}/about",    phone, "GET",  ct),
            FetchAsync(http, $"{BaseUrl}/bizos",    phone, "POST", ct),
            FetchAsync(http, $"{BaseUrl}/devices",  phone, "GET",  ct),
            FetchAsync(http, $"{BaseUrl}/privacy",  phone, "GET",  ct),
            FetchAsync(http, $"{BaseUrl}/wspic/dck",phone, "GET",  ct),
        };

        var results = await Task.WhenAll(tasks);

        AppendSection(sb, "Status / About",        results[0]);
        AppendSection(sb, "Business Verification", results[1]);
        AppendSection(sb, "Linked Devices",        results[2]);
        AppendSection(sb, "Privacy Settings",      results[3]);
        AppendSection(sb, "Full OSINT Data",       results[4]);

        return sb.ToString();
    }

    private static async Task<(string label, string content)> FetchAsync(
        HttpClient http, string url, string phone, string method, CancellationToken ct)
    {
        var label = url.Split('/').Last();
        try
        {
            HttpResponseMessage resp;
            if (method == "POST")
                resp = await http.PostAsync(url, new StringContent(phone), ct);
            else
                resp = await http.GetAsync($"{url}?phone={phone}", ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return (label, "Number not found on WhatsApp or not registered.");

            if ((int)resp.StatusCode == 530)
                return (label, "TOOL_UNAVAILABLE: WhatsApp OSINT API is currently down (provider outage, HTTP 530). Skip this tool.");

            if (!resp.IsSuccessStatusCode)
                return (label, $"API error {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync(ct)}");

            var body = await resp.Content.ReadAsStringAsync(ct);
            return (label, FormatJson(body));
        }
        catch (Exception ex)
        {
            return (label, $"Request failed: {ex.Message}");
        }
    }

    private static string FormatJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "(empty response)";
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var sb = new StringBuilder();
            FlattenJson(doc.RootElement, sb, "");
            return sb.Length > 0 ? sb.ToString().TrimEnd() : raw;
        }
        catch
        {
            return raw.Length > 500 ? raw[..500] + "…" : raw;
        }
    }

    private static void FlattenJson(JsonElement el, StringBuilder sb, string prefix)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenJson(prop.Value, sb, key);
                }
                break;

            case JsonValueKind.Array:
                var items = el.EnumerateArray().ToList();
                for (int i = 0; i < items.Count; i++)
                    FlattenJson(items[i], sb, $"{prefix}[{i}]");
                break;

            case JsonValueKind.String:
                var str = el.GetString();
                // Skip base64 blobs (profile pictures)
                if (str != null && str.Length < 200)
                    sb.AppendLine($"  {prefix,-28}: {str}");
                break;

            case JsonValueKind.Null:
                break;

            default:
                sb.AppendLine($"  {prefix,-28}: {el.GetRawText()}");
                break;
        }
    }

    private static void AppendSection(StringBuilder sb, string title, (string label, string content) result)
    {
        sb.AppendLine();
        sb.AppendLine($"── {title} ──");
        sb.AppendLine(result.content);
    }
}
