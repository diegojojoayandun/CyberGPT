using System.Runtime.CompilerServices;
using System.Text.Json;
using CyberGPT.API.Models;

namespace CyberGPT.API.Services;

public class OsintAgentService(IOllamaService ollama, OsintToolRegistry registry, ILogger<OsintAgentService> logger)
{
    private const int MaxIterations = 12;

    private static readonly string SystemPrompt = """
        You are an expert OSINT analyst. Given a target (domain, IP, email or username),
        use ALL available tools to gather comprehensive intelligence systematically.

        Investigation strategy:
        - For DOMAINS:
            1. whois_lookup (registrar, dates, nameservers)
            2. dns_lookup with record_type=ALL (A, MX, TXT, NS, CNAME)
            3. subdomain_discovery (passive recon via crt.sh)
            4. geoip_lookup on the main A record IP
            5. shodan_lookup on the main A record IP (open ports, CVEs, services)
            6. virustotal_lookup with target_type=domain (reputation, detections)

        - For IPs:
            1. whois_lookup
            2. geoip_lookup
            3. shodan_lookup (open ports, banners, CVEs)
            4. virustotal_lookup with target_type=ip

        - For file hashes (MD5/SHA1/SHA256):
            1. virustotal_lookup with target_type=hash

        - For phone numbers (digits only, 8-15 chars, with country code):
            1. whatsapp_osint ONLY — do NOT run whois_lookup, dns_lookup, geoip_lookup or shodan_lookup on a phone number, they are meaningless for phone numbers.
            2. If whatsapp_osint returns TOOL_UNAVAILABLE, write the report immediately stating the service is down.

        - For unknown targets: infer type and apply the right tools.
          A string of 8-15 digits is likely a phone number → use whatsapp_osint ONLY.
          Never apply domain/IP tools to phone numbers.
        - Never call the same tool twice with the same arguments.
        - If a tool returns "TOOL_UNAVAILABLE", do NOT retry it — skip it and move on.
        - After ALL relevant tools have been used (or skipped), write the final report.

        When done, produce a structured intelligence report in Spanish with:
        ## Resumen ejecutivo
        ## Hallazgos técnicos (one subsection per tool used)
        ## Indicadores de riesgo
        ## Conclusiones y recomendaciones
        """;

    /// <summary>
    /// Runs the OSINT agent loop and streams events as SSE-ready objects.
    /// Event shapes:
    ///   { type:"thinking", content:"..." }
    ///   { type:"tool_start", tool:"...", target:"..." }
    ///   { type:"tool_done",  tool:"...", result:"..." }
    ///   { type:"report",     content:"..." }
    ///   { type:"error",      content:"..." }
    ///   { type:"done" }
    /// </summary>
    public async IAsyncEnumerable<object> InvestigateAsync(
        string target, string targetType,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        logger.LogInformation("[OSINT] Starting investigation: {Target} ({Type})", target, targetType);

        var tools    = registry.ToOllamaSchema();
        var messages = new List<object>
        {
            new { role = "system",  content = SystemPrompt },
            new { role = "user",    content = $"Investigate this target: {target}\nTarget type: {targetType}" }
        };

        // Track (tool, args) pairs already called to prevent infinite retry loops
        var calledTools = new HashSet<string>();

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            if (ct.IsCancellationRequested) yield break;

            (string content, List<LlmToolCall> toolCalls) response;
            string? llmError = null;
            try
            {
                response = await ollama.ChatWithToolsAsync(messages, tools, ct: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[OSINT] LLM call failed");
                llmError = ex.Message;
                response = default;
            }

            if (llmError != null)
            {
                yield return new { type = "error", content = $"LLM error: {llmError}" };
                yield break;
            }

            var (rawContent, respToolCalls) = response;
            // Strip any stray </think> tags the model may leave in the content
            var respContent = System.Text.RegularExpressions.Regex
                .Replace(rawContent ?? "", @"</?think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .TrimStart('\n', '\r', ' ');
            respToolCalls ??= [];

            // Emit any thinking/intermediate content
            if (!string.IsNullOrWhiteSpace(respContent) && respToolCalls.Count == 0)
            {
                // No more tool calls → this is the final report
                yield return new { type = "report", content = respContent };
                yield return new { type = "done" };
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(respContent))
                yield return new { type = "thinking", content = respContent };

            if (respToolCalls.Count == 0)
            {
                yield return new { type = "error", content = "Agent stopped without producing a report." };
                yield break;
            }

            // Add the assistant message (with tool_calls) to conversation
            messages.Add(new
            {
                role       = "assistant",
                content    = respContent,
                tool_calls = respToolCalls.Select(tc => new
                {
                    function = new { name = tc.Name, arguments = tc.Arguments }
                }).ToList<object>()
            });

            // Execute each tool and feed results back
            foreach (var call in respToolCalls)
            {
                if (ct.IsCancellationRequested) yield break;

                var tool = registry.Get(call.Name);
                if (tool == null)
                {
                    logger.LogWarning("[OSINT] Unknown tool: {Name}", call.Name);
                    messages.Add(new { role = "tool", name = call.Name, content = $"Unknown tool: {call.Name}" });
                    continue;
                }

                // Deduplicate: skip if this exact (tool, args) was already called
                var callKey = $"{call.Name}:{call.Arguments.GetRawText()}";
                if (!calledTools.Add(callKey))
                {
                    logger.LogWarning("[OSINT] Duplicate tool call skipped: {Key}", callKey);
                    messages.Add(new { role = "tool", name = call.Name, content = "Already called with these arguments. Use a different tool or write the final report." });
                    continue;
                }

                // Extract a readable target label for the UI
                var targetLabel = TryGetStringArg(call.Arguments, "target")
                               ?? TryGetStringArg(call.Arguments, "domain")
                               ?? target;

                yield return new { type = "tool_start", tool = call.Name, target = targetLabel };
                logger.LogInformation("[OSINT] Executing {Tool}({Target})", call.Name, targetLabel);

                string result;
                try
                {
                    result = await tool.ExecuteAsync(call.Arguments, ct);
                }
                catch (Exception ex)
                {
                    result = $"Tool execution error: {ex.Message}";
                    logger.LogError(ex, "[OSINT] Tool {Tool} failed", call.Name);
                }

                yield return new { type = "tool_done", tool = call.Name, result };
                logger.LogInformation("[OSINT] {Tool} done ({Bytes} chars)", call.Name, result.Length);

                // Add tool result to conversation
                messages.Add(new { role = "tool", name = call.Name, content = result });
            }
        }

        yield return new { type = "error", content = "Max iterations reached without a final report." };
    }

    private static string? TryGetStringArg(JsonElement args, string key)
    {
        try { return args.GetProperty(key).GetString(); } catch { return null; }
    }
}
