using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Models;
using CodeLensAI.Options;
using Newtonsoft.Json;

namespace CodeLensAI.Services
{
    /// <summary>
    /// Communicates with any OpenAI-compatible local LLM endpoint
    /// (Ollama, LM Studio, llama.cpp server, etc.).
    /// Thread-safe singleton; HttpClient is reused across calls.
    /// </summary>
    public sealed class LlmService : IDisposable
    {
        private static readonly string SystemPrompt =
            "You are an expert software developer and code analyst. " +
            "When given code, you analyze it carefully and provide clear, actionable " +
            "explanations, bug fixes, refactoring suggestions, or answers to questions. " +
            "Respond in the same language the user writes in. " +
            "Format code blocks with appropriate markdown fences.";

        private HttpClient _httpClient = null!;  // assigned in ctor
        private LlmOptions _options = null!;   // assigned in ctor
        private bool _disposed;

        public LlmService(LlmOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = BuildHttpClient(options);
        }

        /// <summary>
        /// Rebuilds the HTTP client when options change (endpoint / key / timeout).
        /// Call this after the user saves new settings.
        /// </summary>
        public void RefreshOptions(LlmOptions options)
        {
            ThrowIfDisposed();
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var old = _httpClient;
            _httpClient = BuildHttpClient(options);
            old.Dispose();
        }

        /// <summary>
        /// Sends the selected code + user question to the LLM and returns the response.
        /// </summary>
        /// <param name="selectedCode">Code selected in the editor (may be empty).</param>
        /// <param name="userMessage">Free-form question or instruction from the user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<LlmResult> AnalyzeAsync(
            string selectedCode,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_options.IsConfigured)
                return LlmResult.Fail(
                    "LLM is not configured. Go to Tools → Options → CodeLens AI → LLM Connection.");

            try
            {
                var userContent = BuildUserContent(selectedCode, userMessage);
                var request = new ChatCompletionRequest
                {
                    Model = _options.ModelName,
                    MaxTokens = _options.MaxTokens,
                    Temperature = _options.Temperature,
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.System(SystemPrompt),
                        ChatMessage.User(userContent)
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_options.EndpointUrl}/chat/completions";
                var response = await _httpClient
                    .PostAsync(url, httpContent, cancellationToken)
                    .ConfigureAwait(false);

                var responseBody = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return LlmResult.Fail(
                        $"LLM returned HTTP {(int)response.StatusCode}: {responseBody}");

                var completionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseBody);
                var content = completionResponse?.GetContent();

                return string.IsNullOrEmpty(content)
                    ? LlmResult.Fail("LLM returned an empty response.")
                    : LlmResult.Ok(content!);
            }
            catch (OperationCanceledException)
            {
                return LlmResult.Fail("Request was cancelled.");
            }
            catch (HttpRequestException ex)
            {
                return LlmResult.Fail(
                    $"Cannot reach LLM at {_options.EndpointUrl}.\n" +
                    $"Make sure your local LLM server is running.\n\nDetail: {ex.Message}");
            }
            catch (Exception ex)
            {
                return LlmResult.Fail($"Unexpected error: {ex.Message}");
            }
        }

        private static string BuildUserContent(string selectedCode, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(selectedCode))
                return userMessage;

            return $"{userMessage}\n\n```\n{selectedCode}\n```";
        }

        private static HttpClient BuildHttpClient(LlmOptions options)
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            client.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.ApiKey);

            return client;
        }


        /// <summary>
        /// Test-only factory: creates an LlmService with a pre-built HttpClient
        /// (e.g. backed by a mock handler). Not for production use.
        /// </summary>
        internal static LlmService CreateForTest(LlmOptions options, HttpClient httpClient)
        {
            var service = new LlmService(options);
            // Replace the internally-built client with the test-supplied one
            service._httpClient.Dispose();
            service._httpClient = httpClient;
            return service;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LlmService));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
