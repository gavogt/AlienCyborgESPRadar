using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AlienCyborgESPRadar;

public sealed class RadarAnalysisOrchestrator
{
    private readonly IAgent _summarizer;
    private readonly IAgent _anomaly;
    private readonly IAgent _action;
    private readonly ILogger<RadarAnalysisOrchestrator> _logger;

    public RadarAnalysisOrchestrator(IEnumerable<IAgent> agents, ILogger<RadarAnalysisOrchestrator> logger)
    {
        _summarizer = agents.First(a => a.Name == "Summarizer");
        _anomaly = agents.First(a => a.Name == "AnomalyDetector");
        _action = agents.First(a => a.Name == "ActionAdvisor");
        _logger = logger;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string rawLogs, CancellationToken ct)
    {
        _logger.LogInformation("AnalyzeAsync called. rawLogs length={Len}", rawLogs?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(rawLogs) || rawLogs.Length < 50)
        {
            _logger.LogWarning("Not enough logs to analyze yet.");
            return new AnalysisResult("No logs yet (need more data).", "[]", "Wait for more radar events, then re-run analysis.");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(400));
        var token = cts.Token;

        try
        {
            _logger.LogInformation("Calling Summarizer...");
            var summary = await _summarizer.RunAsync(rawLogs, token);
            _logger.LogInformation("Summarizer done. len={Len}", summary?.Length ?? 0);

            if (LooksLikeNoDataResponse(summary))
            {
                _logger.LogWarning("Summarizer returned no-data style response.");
                return new AnalysisResult("Analyzer didn’t receive usable logs.", "[]", "Check that the dashboard is passing real logs from the database.");
            }

            var anomalyInput = $"RAW LOGS:\n{rawLogs}\n\nSUMMARY:\n{summary}";
            _logger.LogInformation("Calling AnomalyDetector...");
            var anomalies = await _anomaly.RunAsync(anomalyInput, token);
            _logger.LogInformation("AnomalyDetector done. len={Len}", anomalies?.Length ?? 0);

            if (LooksLikeNoDataResponse(anomalies))
            {
                _logger.LogWarning("AnomalyDetector returned no-data style response.");
                return new AnalysisResult(summary, "[]", "Anomaly agent did not return usable output. Verify prompt + input size.");
            }

            var actionInput = $"SUMMARY:\n{summary}\n\nANOMALIES:\n{anomalies}";
            _logger.LogInformation("Calling ActionAdvisor...");
            var actions = await _action.RunAsync(actionInput, token);
            _logger.LogInformation("ActionAdvisor done. len={Len}", actions?.Length ?? 0);

            if (LooksLikeNoDataResponse(actions))
                actions = "No recommended actions returned (agent may not have received usable anomaly data).";

            return new AnalysisResult(summary, anomalies, actions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Analysis canceled (timeout or navigation).");
            return new AnalysisResult("Analysis canceled (navigation or timeout).", "[]", "Try again—if it keeps timing out, reduce log count or lower max tokens.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed.");
            return new AnalysisResult("Analysis failed.", "[]", "Check server logs for details.");
        }
    }

    private static bool LooksLikeNoDataResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        var t = text.ToLowerInvariant();
        return t.Contains("please paste") ||
               t.Contains("share the actual") ||
               t.Contains("haven't included") ||
               t.Contains("need to see") ||
               t.Contains("no actual log") ||
               t.Contains("provide the raw logs");
    }
}