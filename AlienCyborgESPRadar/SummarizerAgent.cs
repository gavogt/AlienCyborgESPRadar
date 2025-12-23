using AlienCyborgESPRadar;
using AlienCyborgESPRadar.Services;

public sealed class SummarizerAgent(LmStudioClient llm) : IAgent
{
    public string Name => "Summarizer";

    public Task<string> RunAsync(string input, CancellationToken ct)
        => llm.ChatAsync(
            model: "your-model-name-here",
            messages: new[]
            {
                ("system", "You summarize radar motion logs. Output: short summary + key stats. Be concise."),
                ("user", input)
            },
            ct: ct);
}