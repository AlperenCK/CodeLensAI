using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CodeLensAI.Commands;
using CodeLensAI.Models;
using CodeLensAI.Options;
using CodeLensAI.Services;
using CodeLensAI.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatWindow), Style = VsDockStyle.Tabbed, Window = "DocumentWell")]
    [ProvideOptionPage(typeof(LlmOptions), "CodeLens AI", "LLM Connection", 0, 0, true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string,
        PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSPackage : AsyncPackage, ILlmHost
    {
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        private LlmService? _llmService;

        public LlmOptions GetOptions() =>
            (LlmOptions)GetDialogPage(typeof(LlmOptions));

        public async Task<LlmResult> AnalyzeAsync(
            string selectedCode,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var options = GetOptions();
            if (_llmService == null)
                _llmService = new LlmService(options);
            else
                _llmService.RefreshOptions(options);

            return await _llmService
                .AnalyzeAsync(selectedCode, userMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Switch to UI thread using ThreadHelper — no JoinableTaskFactory needed
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await AnalyzeCommand.InitializeAsync(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _llmService?.Dispose();
            base.Dispose(disposing);
        }
    }
}
