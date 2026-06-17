using CodeLensAI.Options;
using Xunit;

#nullable disable  // Test files — nullable strictness not required


namespace CodeLensAI.Tests
{
    /// <summary>
    /// Unit tests for <see cref="LlmOptions"/> — validation logic,
    /// default values, and property sanitisation.
    /// </summary>
    public class LlmOptionsTests
    {
        // ── Default values ─────────────────────────────────────────────────

        [Fact]
        public void DefaultEndpointUrl_IsOllamaLocalhost()
        {
            var opts = new LlmOptions();
            Assert.Equal("http://localhost:11434/v1", opts.EndpointUrl);
        }

        [Fact]
        public void DefaultModelName_IsCodellama()
        {
            var opts = new LlmOptions();
            Assert.Equal("codellama", opts.ModelName);
        }

        [Fact]
        public void DefaultApiKey_IsEmpty()
        {
            var opts = new LlmOptions();
            Assert.Equal(string.Empty, opts.ApiKey);
        }

        [Fact]
        public void DefaultMaxTokens_Is2048()
        {
            var opts = new LlmOptions();
            Assert.Equal(2048, opts.MaxTokens);
        }

        [Fact]
        public void DefaultTemperature_Is0Point2()
        {
            var opts = new LlmOptions();
            Assert.Equal(0.2, opts.Temperature);
        }

        [Fact]
        public void DefaultTimeoutSeconds_Is60()
        {
            var opts = new LlmOptions();
            Assert.Equal(60, opts.TimeoutSeconds);
        }

        // ── IsConfigured ───────────────────────────────────────────────────

        [Fact]
        public void IsConfigured_WithDefaults_ReturnsTrue()
        {
            var opts = new LlmOptions();
            Assert.True(opts.IsConfigured);
        }

        [Fact]
        public void IsConfigured_EmptyEndpointUrl_ReturnsFalse()
        {
            var opts = new LlmOptions { EndpointUrl = "   " };
            // Setter normalises whitespace-only to default, so we set via reflection
            var field = typeof(LlmOptions)
                .GetField("_endpointUrl",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(opts, string.Empty);
            Assert.False(opts.IsConfigured);
        }

        [Fact]
        public void IsConfigured_EmptyModelName_ReturnsFalse()
        {
            var opts = new LlmOptions();
            var field = typeof(LlmOptions)
                .GetField("_modelName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(opts, string.Empty);
            Assert.False(opts.IsConfigured);
        }

        [Fact]
        public void IsConfigured_HttpsEndpoint_ReturnsTrue()
        {
            var opts = new LlmOptions { EndpointUrl = "https://my-llm.local/v1" };
            Assert.True(opts.IsConfigured);
        }

        [Fact]
        public void IsConfigured_FtpEndpoint_ReturnsFalse()
        {
            var opts = new LlmOptions();
            var field = typeof(LlmOptions)
                .GetField("_endpointUrl",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(opts, "ftp://localhost/v1");
            Assert.False(opts.IsConfigured);
        }

        // ── EndpointUrl sanitisation ───────────────────────────────────────

        [Fact]
        public void EndpointUrl_SetWithTrailingSlash_IsStripped()
        {
            var opts = new LlmOptions { EndpointUrl = "http://localhost:11434/v1/" };
            Assert.Equal("http://localhost:11434/v1", opts.EndpointUrl);
        }

        [Fact]
        public void EndpointUrl_SetNullOrWhitespace_FallsBackToDefault()
        {
            var opts = new LlmOptions { EndpointUrl = null! };
            Assert.Equal("http://localhost:11434/v1", opts.EndpointUrl);

            opts.EndpointUrl = "   ";
            Assert.Equal("http://localhost:11434/v1", opts.EndpointUrl);
        }

        // ── ModelName sanitisation ─────────────────────────────────────────

        [Fact]
        public void ModelName_SetNullOrWhitespace_FallsBackToDefault()
        {
            var opts = new LlmOptions { ModelName = null! };
            Assert.Equal("codellama", opts.ModelName);
        }

        [Fact]
        public void ModelName_SetWithWhitespace_IsTrimmed()
        {
            var opts = new LlmOptions { ModelName = "  deepseek-coder:6.7b  " };
            Assert.Equal("deepseek-coder:6.7b", opts.ModelName);
        }

        // ── MaxTokens clamping ─────────────────────────────────────────────

        [Theory]
        [InlineData(0, 64)]      // below minimum → clamped to 64
        [InlineData(63, 64)]     // just below minimum
        [InlineData(64, 64)]     // at minimum
        [InlineData(512, 512)]   // normal
        [InlineData(8192, 8192)] // at maximum
        [InlineData(9000, 8192)] // above maximum → clamped to 8192
        public void MaxTokens_Clamped(int input, int expected)
        {
            var opts = new LlmOptions { MaxTokens = input };
            Assert.Equal(expected, opts.MaxTokens);
        }

        // ── Temperature clamping ───────────────────────────────────────────

        [Theory]
        [InlineData(-0.1, 0.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(0.5, 0.5)]
        [InlineData(1.0, 1.0)]
        [InlineData(1.1, 1.0)]
        public void Temperature_Clamped(double input, double expected)
        {
            var opts = new LlmOptions { Temperature = input };
            Assert.Equal(expected, opts.Temperature);
        }

        // ── TimeoutSeconds clamping ────────────────────────────────────────

        [Theory]
        [InlineData(0, 5)]
        [InlineData(4, 5)]
        [InlineData(5, 5)]
        [InlineData(60, 60)]
        [InlineData(300, 300)]
        [InlineData(301, 300)]
        public void TimeoutSeconds_Clamped(int input, int expected)
        {
            var opts = new LlmOptions { TimeoutSeconds = input };
            Assert.Equal(expected, opts.TimeoutSeconds);
        }
    }
}
