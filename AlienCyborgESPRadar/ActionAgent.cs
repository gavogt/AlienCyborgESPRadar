using AlienCyborgESPRadar.Services;

namespace AlienCyborgESPRadar
{
    public class ActionAgent(LmStudioClient llm) : IAgent
    {
        public string Name => "ActionAdvisor";

        public Task<string> RunAsync(string input, CancellationToken ct)
        => llm.ChatAsync(
            model: "your-model-name-here",
            messages: new[]
            {
                ("system", "Based on radar logs, suggest actions to improve detection accuracy and reduce false positives. Output JSON with fields: recommended_actions[], priority, notes."),
                ("user", input)
            },
            ct: ct);

    }
}
