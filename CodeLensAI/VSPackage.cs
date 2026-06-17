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
    /// <summary>
    /// Main VSIX package entry point for CodeLens AI extension.
    /// Implements <see cref="ILlmHost"/> so it can be injected into UI controls.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatWindow), Style = VsDockStyle.Tabbed, Window = "DocumentWell")]
    [ProvideOptionPage(typeof(LlmOptions), "CodeLens AI", "LLM Connection", 0, 0, true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string,
        PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSPackage : AsyncPackage, ILlmHost
    {
        /// <summary>Package GUID — must match .vsixmanifest</summary>
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        private LlmService? _llmService;

        // ── ILlmHost ──────────────────────────────────────────────────────────

        /// <inheritdoc />
        public LlmOptions GetOptions() =>
            (LlmOptions)GetDialogPage(typeof(LlmOptions));

        /// <inheritdoc />
        public async Task<LlmResult> AnalyzeAsync(
            string selectedCode,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var options = GetOptions();

            // Rebuild service if options may have changed
            if (_llmService == null)
                _llmService = new LlmService(options);
            else
                _llmService.RefreshOptions(options);

            return await _llmService
                .AnalyzeAsync(selectedCode, userMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        // ── AsyncPackage ──────────────────────────────────────────────────────

        /// <inheritdoc />
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AnalyzeCommand.InitializeAsync(this);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _llmService?.Dispose();

            base.Dispose(disposing);
        }
    }
}
