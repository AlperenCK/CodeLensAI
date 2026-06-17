using System.Collections.Generic;
using Newtonsoft.Json;

namespace CodeLensAI.Models
{
    /// <summary>Single chat message in the OpenAI-compatible format.</summary>
    public sealed class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        public static ChatMessage System(string content) =>
            new ChatMessage { Role = "system", Content = content };

        public static ChatMessage User(string content) =>
            new ChatMessage { Role = "user", Content = content };

        public static ChatMessage Assistant(string content) =>
            new ChatMessage { Role = "assistant", Content = content };
    }

    /// <summary>Request body for /v1/chat/completions.</summary>
    public sealed class ChatCompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 2048;

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.2;

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;
    }

    /// <summary>Minimal response deserialization — we only need the content text.</summary>
    public sealed class ChatCompletionResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; } = new List<Choice>();

        public string? GetContent() =>
            Choices.Count > 0 ? Choices[0].Message?.Content : null;
    }

    public sealed class Choice
    {
        [JsonProperty("message")]
        public ChatMessage? Message { get; set; }

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>Result returned to the UI layer.</summary>
    public sealed class LlmResult
    {
        public bool Success { get; private set; }
        public string Content { get; private set; }
        public string? ErrorMessage { get; private set; }

        private LlmResult(bool success, string content, string? error)
        {
            Success = success;
            Content = content;
            ErrorMessage = error;
        }

        public static LlmResult Ok(string content) =>
            new LlmResult(true, content, null);

        public static LlmResult Fail(string error) =>
            new LlmResult(false, string.Empty, error);
    }
}
