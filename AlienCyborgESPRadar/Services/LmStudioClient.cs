using System.Net.Http.Json;
namespace AlienCyborgESPRadar.Services
{
    public sealed class LmStudioClient
    {
        private readonly HttpClient _http;

        public LmStudioClient(HttpClient http) => _http = http;

        public async Task<string> ChatAsync(string model, IEnumerable<(string role, string content)> messages,
            double temperature = 0.2, int maxTokens = 600, CancellationToken ct = default)
        {
            try
            {
                var payload = new
                {
                    model,
                    temperature,
                    max_tokens = maxTokens,
                    messages = messages.Select(m => new { role = m.role, content = m.content })
                };

                using var resp = await _http.PostAsJsonAsync("chat/completions", payload, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
                return json?.Choices?.FirstOrDefault()?.message?.content ?? "";
            }
            catch (OperationCanceledException)
            {
                return "";
            }
        }
    }
}