namespace ChatApp2.Services
{
    public sealed class OllamaOptions
    {
        public string? BaseUrl { get; set; } // 例: http://localhost:11434

        public string Model { get; set; } = "llama3";

        public double? Temperature { get; set; } = 0.2;

        public double? TopP { get; set; }

        public int? TopK { get; set; }

        public int? MaxTokens { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        public string? SystemPrompt { get; set; }

        // SystemPromptの最大文字数（既定 10000）
        public int MaxSystemPromptChars { get; set; } = 10000;
    }
}