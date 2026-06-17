using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Models;
using CodeLensAI.Options;

namespace CodeLensAI.Services
{
    /// <summary>
    /// Abstraction over the package-level LLM host.
    /// Allows ChatWindowControl to be tested without a live VSPackage.
    /// </summary>
    public interface ILlmHost
    {
        /// <summary>Current LLM connection options.</summary>
        LlmOptions GetOptions();

        /// <summary>Send selected code + user message to the LLM.</summary>
        Task<LlmResult> AnalyzeAsync(
            string selectedCode,
            string userMessage,
            CancellationToken cancellationToken = default);
    }
}
