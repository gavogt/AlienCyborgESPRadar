using System.Threading;
using System.Threading.Tasks;
using AlienCyborgESPRadar.Services;

namespace AlienCyborgESPRadar
{
    public sealed class SummarizerAgent : IAgent
    {
        private readonly LmStudioClient _llm;

        public SummarizerAgent(LmStudioClient llm) => _llm = llm;

        public string Name => "Summarizer";

        public Task<string> RunAsync(string input, CancellationToken ct)
            => _llm.ChatAsync(
                model: "openai/gpt-oss-20b",
                messages: new[]
                {
                    ("system", "You summarize radar motion logs. Output: short summary + key stats. Be concise. Please " +
                    "provide the output as a tiny structured format instead of markdown"),
                    ("user", input)
                },
                ct: ct);
    }
}
