using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI.Options
{
    /// <summary>
    /// Options page for CodeLens AI, accessible via
    /// Tools → Options → CodeLens AI → LLM Connection.
    /// Settings are automatically persisted to the VS Settings Store.
    /// </summary>
    public class LlmOptions : DialogPage
    {
        private string _endpointUrl = "http://localhost:11434/v1";
        private string _modelName = "codellama";
        private string _apiKey = string.Empty;
        private int _maxTokens = 4096;
        private double _temperature = 0.2;
        private int _timeoutSeconds = 60;

        /// <summary>
        /// Base URL of the OpenAI-compatible local LLM endpoint.
        /// Examples: http://localhost:11434/v1 (Ollama),
        ///           http://localhost:1234/v1 (LM Studio)
        /// </summary>
        [Category("Connection")]
        [DisplayName("Endpoint URL")]
        [Description(
            "Base URL of your local LLM's OpenAI-compatible API. " +
            "Examples: http://localhost:11434/v1 (Ollama), " +
            "http://localhost:1234/v1 (LM Studio).")]
        public string EndpointUrl
        {
            get => _endpointUrl;
            set => _endpointUrl = string.IsNullOrWhiteSpace(value)
                ? "http://localhost:11434/v1"
                : value.TrimEnd('/');
        }

        /// <summary>Name of the model to invoke (e.g. codellama, deepseek-coder).</summary>
        [Category("Connection")]
        [DisplayName("Model Name")]
        [Description("The model identifier to use (e.g. codellama, deepseek-coder:6.7b).")]
        public string ModelName
        {
            get => _modelName;
            set => _modelName = string.IsNullOrWhiteSpace(value) ? "codellama" : value.Trim();
        }

        /// <summary>
        /// Optional API key. Leave empty for local endpoints that don't require authentication.
        /// </summary>
        [Category("Connection")]
        [DisplayName("API Key (optional)")]
        [Description("Optional API key. Leave empty for Ollama / LM Studio local endpoints.")]
        [PasswordPropertyText(true)]
        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value ?? string.Empty;
        }

        private string _modelProfiles = string.Empty;

        [Category("Models")]
        [DisplayName("Model Profiles")]
        [Description(
            "Define multiple model names separated by newline, semicolon, or comma. " +
            "Colon (:) is NOT a separator (model names may contain it). " +
            "Click the model pill to cycle through profiles. " +
            "Example: gpt-oss-120b;gpt-oss:20b  or one per line")]
        public string ModelProfiles
        {
            get => _modelProfiles;
            set => _modelProfiles = value ?? string.Empty;
        }

        /// <summary>Maximum tokens the model may generate per response.</summary>
        [Category("Generation")]
        [DisplayName("Max Tokens")]
        [Description("Maximum number of tokens the model will generate per response (default: 2048).")]
        public int MaxTokens
        {
            get => _maxTokens;
            set => _maxTokens = value < 64 ? 64 : value > 32768 ? 32768 : value;
        }

        /// <summary>Temperature controlling response creativity (0.0–1.0).</summary>
        [Category("Generation")]
        [DisplayName("Temperature")]
        [Description("Sampling temperature (0.0 = deterministic, 1.0 = creative). Default: 0.2 for code.")]
        public double Temperature
        {
            get => _temperature;
            set => _temperature = value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        }

        /// <summary>HTTP request timeout in seconds.</summary>
        [Category("Connection")]
        [DisplayName("Timeout (seconds)")]
        [Description("HTTP request timeout in seconds (default: 60).")]
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => _timeoutSeconds = value < 5 ? 5 : value > 300 ? 300 : value;
        }

        /// <summary>Quick validation: returns true if the endpoint URL looks usable.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(EndpointUrl) &&
            !string.IsNullOrWhiteSpace(ModelName) &&
            (EndpointUrl.StartsWith("http://") || EndpointUrl.StartsWith("https://"));
    }
}
