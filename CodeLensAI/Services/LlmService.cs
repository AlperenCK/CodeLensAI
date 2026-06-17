using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CodeLensAI.Models;
using CodeLensAI.Options;

namespace CodeLensAI.Services
{
    /// <summary>
    /// Communicates with any OpenAI-compatible local LLM endpoint.
    /// Uses DataContractJsonSerializer — no external NuGet dependencies.
    /// </summary>
    public sealed class LlmService : IDisposable
    {
        private static readonly string SystemPrompt =
            "You are an expert software developer and code analyst. " +
            "When given code, you analyze it carefully and provide clear, actionable " +
            "explanations, bug fixes, refactoring suggestions, or answers to questions. " +
            "Respond in the same language the user writes in. " +
            "Format code blocks with appropriate markdown fences.";

        private HttpClient _httpClient = null!;
        private LlmOptions _options = null!;
        private bool _disposed;

        public LlmService(LlmOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = BuildHttpClient(options);
        }

        public void RefreshOptions(LlmOptions options)
        {
            ThrowIfDisposed();
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var old = _httpClient;
            _httpClient = BuildHttpClient(options);
            old.Dispose();
        }

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
                var userContent = string.IsNullOrWhiteSpace(selectedCode)
                    ? userMessage
                    : $"{userMessage}\n\n```\n{selectedCode}\n```";

                var request = new ChatCompletionRequest
                {
                    Model       = _options.ModelName,
                    MaxTokens   = _options.MaxTokens,
                    Temperature = _options.Temperature,
                    Messages    = new List<ChatMessage>
                    {
                        ChatMessage.System(SystemPrompt),
                        ChatMessage.User(userContent)
                    }
                };

                var json    = Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url     = $"{_options.EndpointUrl}/chat/completions";

                var response = await _httpClient
                    .PostAsync(url, content, cancellationToken)
                    .ConfigureAwait(false);

                var body = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return LlmResult.Fail(
                        $"LLM returned HTTP {(int)response.StatusCode}: {body}");

                var completion = Deserialize<ChatCompletionResponse>(body);
                var text       = completion?.GetContent();

                return string.IsNullOrEmpty(text)
                    ? LlmResult.Fail("LLM returned an empty response.")
                    : LlmResult.Ok(text!);
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

        private static string Serialize<T>(T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using var ms   = new MemoryStream();
            serializer.WriteObject(ms, obj);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static T? Deserialize<T>(string json) where T : class
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using var ms   = new MemoryStream(Encoding.UTF8.GetBytes(json));
                return (T?)serializer.ReadObject(ms);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Test-only factory.</summary>
        internal static LlmService CreateForTest(LlmOptions options, HttpClient httpClient)
        {
            var service = new LlmService(options);
            service._httpClient.Dispose();
            service._httpClient = httpClient;
            return service;
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
