using System;
using System.ComponentModel.Design;
using CodeLensAI.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace CodeLensAI.Commands
{
    /// <summary>
    /// "Analyze with CodeLens AI" command.
    /// Registered via Tools menu and editor context menu.
    /// Grabs selected text, opens the ChatWindow, and pre-fills the code area.
    /// </summary>
    internal sealed class AnalyzeCommand
    {
        // Command set GUID — must match the .vsct / Menus.ctmenu
        public static readonly Guid CommandSet =
            new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        public const int CommandId = 0x0100;

        private readonly AsyncPackage _package;

        private AnalyzeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandId);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        /// <summary>Registers the command. Called from VSPackage.InitializeAsync.</summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService
                ?? throw new InvalidOperationException("Could not obtain IMenuCommandService.");

            _ = new AnalyzeCommand(package, commandService);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            // Command is always visible; enabled only when editor is active
            if (sender is OleMenuCommand cmd)
                cmd.Enabled = true;
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var selectedText = GetSelectedTextFromEditor();
                await OpenChatWindowWithCodeAsync(selectedText);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"CodeLens AI error: {ex.Message}",
                    "CodeLens AI",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string GetSelectedTextFromEditor()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = _package.GetService(typeof(SVsTextManager)) as IVsTextManager2;
            if (textManager == null) return string.Empty;

            textManager.GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out var textView);
            if (textView == null) return string.Empty;

            textView.GetSelectedText(out var selectedText);
            return selectedText ?? string.Empty;
        }

        private async Task OpenChatWindowWithCodeAsync(string selectedCode)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var window = await _package.ShowToolWindowAsync(
                typeof(ChatWindow),
                id: 0,
                create: true,
                cancellationToken: _package.DisposalToken) as ChatWindow;

            if (window?.Frame == null)
                throw new InvalidOperationException("Could not create or show the CodeLens AI Chat window.");

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            // Pass the selected code to the window's control
            if (window.Content is ChatWindowControl control)
                control.SetSelectedCode(selectedCode);
        }
    }
}
