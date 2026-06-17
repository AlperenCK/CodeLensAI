using System;
using System.Runtime.InteropServices;
using System.Threading;
using CodeLensAI.Commands;
using CodeLensAI.Options;
using CodeLensAI.Services;
using CodeLensAI.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodeLensAI
{
    /// <summary>
    /// Main VSIX package entry point for CodeLens AI extension.
    /// Registers commands, tool windows, and options pages.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatWindow), Style = VsDockStyle.Tabbed, Window = "DocumentWell")]
    [ProvideOptionPage(typeof(LlmOptions), "CodeLens AI", "LLM Connection", 0, 0, true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string,
        PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSPackage : AsyncPackage
    {
        /// <summary>Package GUID — must match .vsixmanifest</summary>
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        // Singleton LLM service shared across the package lifetime
        private LlmService? _llmService;

        /// <summary>Global LLM service instance (lazy-created after options load).</summary>
        internal LlmService LlmService => _llmService ??= new LlmService(GetOptions());

        /// <summary>Quick accessor for saved LLM options.</summary>
        internal LlmOptions GetOptions() =>
            (LlmOptions)GetDialogPage(typeof(LlmOptions));

        /// <inheritdoc />
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Switch to UI thread for command registration
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Register the Analyze command
            await AnalyzeCommand.InitializeAsync(this);
        }
    }
}
