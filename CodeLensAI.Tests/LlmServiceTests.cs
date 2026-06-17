using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Models;
using CodeLensAI.Options;
using CodeLensAI.Services;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace CodeLensAI.Tests
{
    /// <summary>
    /// Unit tests for <see cref="LlmService"/> using a mocked <see cref="HttpMessageHandler"/>.
    /// Tests cover success path, HTTP errors, cancellation, and unconfigured state.
    /// </summary>
    public class LlmServiceTests
    {
        // ── Helpers ────────────────────────────────────────────────────────

        private static LlmOptions DefaultOptions() => new LlmOptions
        {
            EndpointUrl  = "http://localhost:11434/v1",
            ModelName    = "codellama",
            MaxTokens    = 512,
            Temperature  = 0.2,
            TimeoutSeconds = 30
        };

        private static string OkResponse(string content) =>
            JsonConvert.SerializeObject(new
            {
                choices = new[]
                {
                    new
                    {
                        message = new { role = "assistant", content },
                        finish_reason = "stop"
                    }
                }
            });

        /// <summary>
        /// Creates a mock HttpMessageHandler that returns the given status + body.
        /// Uses a real HttpClient internally so LlmService's own BuildHttpClient is bypassed
        /// by subclassing via reflection.
        /// </summary>
        private static (LlmService service, Mock<HttpMessageHandler> handlerMock)
            BuildService(HttpStatusCode status, string responseBody, LlmOptions? options = null)
        {
            var opts = options ?? DefaultOptions();

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content    = new StringContent(responseBody, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object)
            {
                Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds)
            };

            // Inject via the internal test constructor we add below
            var service = LlmService.CreateForTest(opts, httpClient);
            return (service, handlerMock);
        }

        // ── Success path ───────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_ValidResponse_ReturnsSuccess()
        {
            var (service, _) = BuildService(HttpStatusCode.OK, OkResponse("Use a StringBuilder."));

            var result = await service.AnalyzeAsync("var s = \"\";", "How to improve?");

            Assert.True(result.Success);
            Assert.Equal("Use a StringBuilder.", result.Content);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_NoCode_SendsOnlyUserMessage()
        {
            HttpRequestMessage? captured = null;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content    = new StringContent(OkResponse("OK"), Encoding.UTF8, "application/json")
                });

            var opts   = DefaultOptions();
            var client = new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(30) };
            var service = LlmService.CreateForTest(opts, client);

            await service.AnalyzeAsync(string.Empty, "What is SOLID?");

            Assert.NotNull(captured);
            var body = await captured!.Content!.ReadAsStringAsync();
            // No code fence should appear when selectedCode is empty
            Assert.DoesNotContain("```", body);
            Assert.Contains("What is SOLID?", body);
        }

        [Fact]
        public async Task AnalyzeAsync_WithCode_IncludesCodeFence()
        {
            HttpRequestMessage? captured = null;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content    = new StringContent(OkResponse("OK"), Encoding.UTF8, "application/json")
                });

            var opts    = DefaultOptions();
            var client  = new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(30) };
            var service = LlmService.CreateForTest(opts, client);

            await service.AnalyzeAsync("int x = 1;", "Explain.");

            var body = await captured!.Content!.ReadAsStringAsync();
            Assert.Contains("```", body);
            Assert.Contains("int x = 1;", body);
        }

        // ── HTTP error handling ────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_Http500_ReturnsFailure()
        {
            var (service, _) = BuildService(HttpStatusCode.InternalServerError, "Server error");

            var result = await service.AnalyzeAsync("code", "question");

            Assert.False(result.Success);
            Assert.Contains("500", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_Http401_ReturnsFailure()
        {
            var (service, _) = BuildService(HttpStatusCode.Unauthorized, "Unauthorized");

            var result = await service.AnalyzeAsync("code", "question");

            Assert.False(result.Success);
            Assert.Contains("401", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeAsync_EmptyChoices_ReturnsFailure()
        {
            var emptyResponse = JsonConvert.SerializeObject(new { choices = Array.Empty<object>() });
            var (service, _) = BuildService(HttpStatusCode.OK, emptyResponse);

            var result = await service.AnalyzeAsync("code", "question");

            Assert.False(result.Success);
            Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ── Unconfigured options ───────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_NotConfigured_ReturnsFailureWithHelpText()
        {
            var opts = new LlmOptions();
            // Force IsConfigured = false by clearing endpoint via backing field
            var field = typeof(LlmOptions)
                .GetField("_endpointUrl",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(opts, string.Empty);

            // HttpClient won't be called but we still need one for constructor
            var service = LlmService.CreateForTest(opts, new HttpClient());

            var result = await service.AnalyzeAsync("code", "question");

            Assert.False(result.Success);
            Assert.Contains("Tools → Options", result.ErrorMessage);
        }

        // ── Cancellation ───────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_CancelledToken_ReturnsFailure()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException());

            var opts    = DefaultOptions();
            var client  = new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(30) };
            var service = LlmService.CreateForTest(opts, client);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await service.AnalyzeAsync("code", "question", cts.Token);

            Assert.False(result.Success);
            Assert.Contains("cancel", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ── Dispose safety ─────────────────────────────────────────────────

        [Fact]
        public async Task AnalyzeAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var (service, _) = BuildService(HttpStatusCode.OK, OkResponse("ok"));
            service.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => service.AnalyzeAsync("code", "question"));
        }
    }
}
