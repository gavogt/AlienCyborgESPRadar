using System.Threading;
using System.Threading.Tasks;
using AlienCyborgESPRadar.Services;

namespace AlienCyborgESPRadar
{
    public class AnomalyAgent : IAgent
    {
        private readonly LmStudioClient _llm;

        public AnomalyAgent(LmStudioClient llm) => _llm = llm;

        public string Name => "AnomalyDetector";

        public Task<string> RunAsync(string input, CancellationToken ct)
            => _llm.ChatAsync(
                model: "qwen/qwen3-coder-30b",
                messages: new[]
                {
                    ("system", "Detect anomalies in radar logs (spikes, repeats, odd timing, likely false positives). Output JSON with fields: severity, anomalies[], notes."),
                    ("user", input)
                },
                ct: ct);
    }
}
