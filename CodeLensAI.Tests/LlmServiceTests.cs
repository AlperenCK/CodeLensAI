#nullable disable  // Test files

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Models;
using CodeLensAI.Options;
using CodeLensAI.Services;
using Newtonsoft.Json;
using Xunit;

namespace CodeLensAI.Tests
{
    /// <summary>
    /// Unit tests for <see cref="LlmService"/> using a simple fake HttpMessageHandler.
    /// No Moq dependency — avoids reflection/proxy issues on net472 CI.
    /// </summary>
    public class LlmServiceTests
    {
        // ── Fake handler ───────────────────────────────────────────────────

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            private readonly bool _throwCancel;
            private readonly bool _throwRequest;
            public HttpRequestMessage LastRequest { get; private set; }

            public FakeHandler(HttpStatusCode status, string body,
                bool throwCancel = false, bool throwRequest = false)
            {
                _status = status;
                _body = body;
                _throwCancel = throwCancel;
                _throwRequest = throwRequest;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                if (_throwCancel) throw new OperationCanceledException();
                if (_throwRequest) throw new HttpRequestException("Connection refused");
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = _status,
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static LlmOptions DefaultOptions() => new LlmOptions
        {
            EndpointUrl   = "http://localhost:11434/v1",
            ModelName     = "codellama",
            MaxTokens     = 512,
            Temperature   = 0.2,
            TimeoutSeconds = 30
        };

        private static string OkResponse(string content) =>
            JsonConvert.SerializeObject(new
            {
                choices = new[]
                {
                    new { message = new { role = "assistant", content }, finish_reason = "stop" }
                }
            });

        private static LlmService Build(FakeHandler handler, LlmOptions opts = null)
        {
            var options = opts ?? DefaultOptions();
            var client  = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            return LlmService.CreateForTest(options, client);
        }

        // ── Success path ───────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_ValidResponse_ReturnsSuccess()
        {
            var handler = new FakeHandler(HttpStatusCode.OK, OkResponse("Use a StringBuilder."));
            using var svc = Build(handler);

            var result = await svc.AnalyzeAsync("var s = \"\";", "How to improve?");

            Assert.True(result.Success);
            Assert.Equal("Use a StringBuilder.", result.Content);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_NoCode_DoesNotIncludeCodeFence()
        {
            var handler = new FakeHandler(HttpStatusCode.OK, OkResponse("OK"));
            using var svc = Build(handler);

            await svc.AnalyzeAsync(string.Empty, "What is SOLID?");

            var body = await handler.LastRequest.Content.ReadAsStringAsync();
            Assert.DoesNotContain("```", body);
            Assert.Contains("What is SOLID?", body);
        }

        [Fact]
        public async Task AnalyzeAsync_WithCode_IncludesCodeFence()
        {
            var handler = new FakeHandler(HttpStatusCode.OK, OkResponse("OK"));
            using var svc = Build(handler);

            await svc.AnalyzeAsync("int x = 1;", "Explain.");

            var body = await handler.LastRequest.Content.ReadAsStringAsync();
            Assert.Contains("```", body);
            Assert.Contains("int x = 1;", body);
        }

        // ── HTTP error handling ────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_Http500_ReturnsFailure()
        {
            using var svc = Build(new FakeHandler(HttpStatusCode.InternalServerError, "Server error"));
            var result = await svc.AnalyzeAsync("code", "question");
            Assert.False(result.Success);
            Assert.Contains("500", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_Http401_ReturnsFailure()
        {
            using var svc = Build(new FakeHandler(HttpStatusCode.Unauthorized, "Unauthorized"));
            var result = await svc.AnalyzeAsync("code", "question");
            Assert.False(result.Success);
            Assert.Contains("401", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_EmptyChoices_ReturnsFailure()
        {
            var empty = JsonConvert.SerializeObject(new { choices = Array.Empty<object>() });
            using var svc = Build(new FakeHandler(HttpStatusCode.OK, empty));
            var result = await svc.AnalyzeAsync("code", "question");
            Assert.False(result.Success);
            Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ── Unconfigured options ───────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_NotConfigured_ReturnsHelpText()
        {
            var opts = new LlmOptions();
            var field = typeof(LlmOptions).GetField("_endpointUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(opts, string.Empty);

            using var svc = Build(new FakeHandler(HttpStatusCode.OK, "{}"), opts);
            var result = await svc.AnalyzeAsync("code", "question");

            Assert.False(result.Success);
            Assert.Contains("Tools", result.ErrorMessage);
        }

        // ── Cancellation ───────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_Cancelled_ReturnsFailure()
        {
            using var svc = Build(new FakeHandler(HttpStatusCode.OK, "{}", throwCancel: true));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await svc.AnalyzeAsync("code", "question", cts.Token);
            Assert.False(result.Success);
            Assert.Contains("cancel", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ── Dispose safety ─────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var svc = Build(new FakeHandler(HttpStatusCode.OK, OkResponse("ok")));
            svc.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => svc.AnalyzeAsync("code", "question"));
        }
    }
}
