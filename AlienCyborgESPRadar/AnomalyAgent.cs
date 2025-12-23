using AlienCyborgESPRadar.Services;

namespace AlienCyborgESPRadar
{
    public class AnomalyAgent(LmStudioClient llm) : IAgent
    {
        public string Name => "AnomalyDetector";
        public Task<string> RunAsync(string input, CancellationToken ct)
            => llm.ChatAsync(
                model: "your-model-name-here",
                messages: new[]
                {
                    ("system", "Detect anomalies in radar logs (spikes, repeats, odd timing, likely false positives). Output JSON with fields: severity, anomalies[], notes."),
                    ("user", input)
                },
                ct: ct);
    }
}
