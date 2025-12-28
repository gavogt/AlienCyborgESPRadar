using System.Threading;
using System.Threading.Tasks;
using AlienCyborgESPRadar.Services;

namespace AlienCyborgESPRadar
{
    public class ActionAgent : IAgent
    {
        private readonly LmStudioClient _llm;

        public ActionAgent(LmStudioClient llm) => _llm = llm;

        public string Name => "ActionAdvisor";

        public Task<string> RunAsync(string input, CancellationToken ct)
            => _llm.ChatAsync(
                model: "openai/gpt-oss-20b",
                messages: new[]
                {
                    ("system", "Based on radar logs, suggest actions to improve detection accuracy and reduce false positives. Output JSON with fields: recommended_actions[], priority, notes."),
                    ("user", input)
                },
                ct: ct);

    }
}
