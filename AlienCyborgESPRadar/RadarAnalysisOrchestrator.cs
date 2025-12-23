namespace AlienCyborgESPRadar
{
    public sealed class RadarAnalysisOrchestrator(IEnumerable<IAgent> agents)
    {
        private readonly IAgent _summarizer = agents.First(a => a.Name == "Summarizer");
        private readonly IAgent _anomaly = agents.First(a => a.Name == "AnomalyDetector");
        private readonly IAgent _action = agents.First(a => a.Name == "ActionAdvisor");

        public async Task<AnalysisResult> AnalyzeAsync(string rawLogs, CancellationToken ct)
        {
            var summary = await _summarizer.RunAsync(rawLogs, ct);
            
            var anomalyInput = $"SUMMARY:\n{summary}\n\nRAW LOGS:\n{rawLogs}";
            var anomalies = await _anomaly.RunAsync(anomalyInput, ct);

            var actionInput = $"SUMMARY:\n{summary}\n\nANOMALIES:\n{anomalies}";
            var actions = await _action.RunAsync(actionInput, ct);

            return new AnalysisResult(summary, anomalies, actions);
        }
    }
}
