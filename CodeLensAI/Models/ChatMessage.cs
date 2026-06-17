using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CodeLensAI.Models
{
    [DataContract]
    public sealed class ChatMessage
    {
        [DataMember(Name = "role")]
        public string Role { get; set; } = "user";

        [DataMember(Name = "content")]
        public string Content { get; set; } = string.Empty;

        public static ChatMessage System(string content) =>
            new ChatMessage { Role = "system", Content = content };

        public static ChatMessage User(string content) =>
            new ChatMessage { Role = "user", Content = content };

        public static ChatMessage Assistant(string content) =>
            new ChatMessage { Role = "assistant", Content = content };
    }

    [DataContract]
    public sealed class ChatCompletionRequest
    {
        [DataMember(Name = "model")]
        public string Model { get; set; } = string.Empty;

        [DataMember(Name = "messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        [DataMember(Name = "max_tokens")]
        public int MaxTokens { get; set; } = 2048;

        [DataMember(Name = "temperature")]
        public double Temperature { get; set; } = 0.2;

        [DataMember(Name = "stream")]
        public bool Stream { get; set; } = false;
    }

    [DataContract]
    public sealed class ChatCompletionResponse
    {
        [DataMember(Name = "choices")]
        public List<Choice> Choices { get; set; } = new List<Choice>();

        public string? GetContent() =>
            Choices.Count > 0 ? Choices[0].Message?.Content : null;
    }

    [DataContract]
    public sealed class Choice
    {
        [DataMember(Name = "message")]
        public ChatMessage? Message { get; set; }

        [DataMember(Name = "finish_reason")]
        public string? FinishReason { get; set; }
    }

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
