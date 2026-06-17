using CodeLensAI.Models;
using Newtonsoft.Json;
using Xunit;

#nullable disable  // Test files — nullable strictness not required


namespace CodeLensAI.Tests
{
    /// <summary>Unit tests for <see cref="ChatMessage"/> and related models.</summary>
    public class ChatMessageTests
    {
        // ── Factory methods ────────────────────────────────────────────────

        [Fact]
        public void ChatMessage_System_HasCorrectRole()
        {
            var msg = ChatMessage.System("You are a coder.");
            Assert.Equal("system", msg.Role);
            Assert.Equal("You are a coder.", msg.Content);
        }

        [Fact]
        public void ChatMessage_User_HasCorrectRole()
        {
            var msg = ChatMessage.User("Fix this.");
            Assert.Equal("user", msg.Role);
        }

        [Fact]
        public void ChatMessage_Assistant_HasCorrectRole()
        {
            var msg = ChatMessage.Assistant("Here is the fix.");
            Assert.Equal("assistant", msg.Role);
        }

        // ── JSON serialisation ─────────────────────────────────────────────

        [Fact]
        public void ChatMessage_SerializesToJson_WithLowercaseRole()
        {
            var msg = ChatMessage.User("hello");
            var json = JsonConvert.SerializeObject(msg);
            Assert.Contains("\"role\":\"user\"", json);
            Assert.Contains("\"content\":\"hello\"", json);
        }

        [Fact]
        public void ChatCompletionRequest_DefaultStream_IsFalse()
        {
            var req = new ChatCompletionRequest();
            Assert.False(req.Stream);
        }

        [Fact]
        public void ChatCompletionRequest_SerializesToJson()
        {
            var req = new ChatCompletionRequest
            {
                Model = "codellama",
                MaxTokens = 512,
                Temperature = 0.3,
                Messages = { ChatMessage.User("test") }
            };

            var json = JsonConvert.SerializeObject(req);
            Assert.Contains("\"model\":\"codellama\"", json);
            Assert.Contains("\"max_tokens\":512", json);
            Assert.Contains("\"stream\":false", json);
        }

        // ── LlmResult ─────────────────────────────────────────────────────

        [Fact]
        public void LlmResult_Ok_IsSuccessWithContent()
        {
            var result = LlmResult.Ok("Great code!");
            Assert.True(result.Success);
            Assert.Equal("Great code!", result.Content);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void LlmResult_Fail_IsNotSuccessWithError()
        {
            var result = LlmResult.Fail("Connection refused.");
            Assert.False(result.Success);
            Assert.Equal(string.Empty, result.Content);
            Assert.Equal("Connection refused.", result.ErrorMessage);
        }

        // ── ChatCompletionResponse deserialization ─────────────────────────

        [Fact]
        public void ChatCompletionResponse_GetContent_ReturnsFirstChoiceContent()
        {
            var json = @"{
                ""choices"": [{
                    ""message"": { ""role"": ""assistant"", ""content"": ""Here is the answer."" },
                    ""finish_reason"": ""stop""
                }]
            }";

            var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(json)!;
            Assert.Equal("Here is the answer.", response.GetContent());
        }

        [Fact]
        public void ChatCompletionResponse_GetContent_EmptyChoices_ReturnsNull()
        {
            var response = new ChatCompletionResponse();
            Assert.Null(response.GetContent());
        }
    }
}
