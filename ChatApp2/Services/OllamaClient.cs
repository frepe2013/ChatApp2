using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ChatApp2.Services
{
    public sealed class OllamaClient : IAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<OllamaOptions> _options;
        private static readonly JsonSerializerOptions SJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public OllamaClient(HttpClient httpClient, IOptionsMonitor<OllamaOptions> options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<string?> AskAsync(string? question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return null;
            }

            var opt = _options.CurrentValue;

            var req = new ChatRequest
            {
                Model = opt.Model,
                Stream = false,
                Options = new GenerationOptions
                {
                    Temperature = opt.Temperature,
                    TopP = opt.TopP,
                    TopK = opt.TopK,
                    MaxTokens = opt.MaxTokens
                }
            };

            if (!string.IsNullOrWhiteSpace(opt.SystemPrompt))
            {
                req.Messages.Add(new Message { Role = "system", Content = opt.SystemPrompt! });
            }

            req.Messages.Add(new Message { Role = "user", Content = question });

            using var response = await _httpClient.PostAsJsonAsync("/api/chat", req, SJsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await SafeReadAsync(response, cancellationToken);
                throw new InvalidOperationException($"Ollama 呼び出しに失敗しました: {(int)response.StatusCode} {response.ReasonPhrase} {detail}");
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(SJsonOptions, cancellationToken);
            return payload?.Message?.Content?.Trim();
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                return (await response.Content.ReadAsStringAsync(ct)).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class ChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<Message> Messages { get; set; } = new();

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("options")]
            public GenerationOptions? Options { get; set; }
        }

        private sealed class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class ChatResponse
        {
            [JsonPropertyName("message")]
            public Message? Message { get; set; }

            [JsonPropertyName("done")]
            public bool Done { get; set; }
        }

        private sealed class GenerationOptions
        {
            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }

            [JsonPropertyName("top_p")]
            public double? TopP { get; set; }

            [JsonPropertyName("top_k")]
            public int? TopK { get; set; }

            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }
        }
    }
}