namespace AlienCyborgESPRadar.Services
{
    internal sealed class ChatResponse
    {
        public sealed class Msg { public string? content { get; set; } }
        public Choice[]? Choices { get; set; }
        public sealed class Choice { public Msg? message { get; set; } }

    }
}
