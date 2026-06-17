using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Models;
using CodeLensAI.Options;

namespace CodeLensAI.Services
{
    /// <summary>Test stub of ILlmHost — no VS SDK dependency.</summary>
    public interface ILlmHost
    {
        LlmOptions GetOptions();
        Task<LlmResult> AnalyzeAsync(string selectedCode, string userMessage,
            CancellationToken cancellationToken = default);
    }
}
