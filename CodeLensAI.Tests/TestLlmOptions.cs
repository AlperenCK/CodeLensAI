#nullable disable

namespace CodeLensAI.Options
{
    /// <summary>
    /// Test stub of LlmOptions — identical API, no VS SDK (DialogPage) dependency.
    /// Compiled directly into the test assembly via the Tests project.
    /// </summary>
    public class LlmOptions
    {
        private string _endpointUrl   = "http://localhost:11434/v1";
        private string _modelName     = "codellama";
        private string _apiKey        = string.Empty;
        private int    _maxTokens     = 2048;
        private double _temperature   = 0.2;
        private int    _timeoutSeconds = 60;

        public string EndpointUrl
        {
            get => _endpointUrl;
            set => _endpointUrl = string.IsNullOrWhiteSpace(value)
                ? "http://localhost:11434/v1"
                : value.TrimEnd('/');
        }

        public string ModelName
        {
            get => _modelName;
            set => _modelName = string.IsNullOrWhiteSpace(value) ? "codellama" : value.Trim();
        }

        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value ?? string.Empty;
        }

        public int MaxTokens
        {
            get => _maxTokens;
            set => _maxTokens = value < 64 ? 64 : value > 8192 ? 8192 : value;
        }

        public double Temperature
        {
            get => _temperature;
            set => _temperature = value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => _timeoutSeconds = value < 5 ? 5 : value > 300 ? 300 : value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(EndpointUrl) &&
            !string.IsNullOrWhiteSpace(ModelName) &&
            (EndpointUrl.StartsWith("http://") || EndpointUrl.StartsWith("https://"));
    }
}
