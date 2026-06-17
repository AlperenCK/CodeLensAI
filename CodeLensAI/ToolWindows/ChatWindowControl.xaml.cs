using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodeLensAI.Services;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI.ToolWindows
{
    /// <summary>
    /// Code-behind for the CodeLens AI chat panel.
    /// Wires UI events to <see cref="LlmService"/> calls via JoinableTaskFactory.
    /// </summary>
    public partial class ChatWindowControl : UserControl
    {
        private CancellationTokenSource? _cts;

        public ChatWindowControl()
        {
            InitializeComponent();
        }

        // ──────────────────────────────────────────
        // Public API — called by AnalyzeCommand
        // ──────────────────────────────────────────

        /// <summary>Pre-fills the code box with text selected in the active editor.</summary>
        public void SetSelectedCode(string code)
        {
            TxtSelectedCode.Text = code;

            if (!string.IsNullOrWhiteSpace(code))
            {
                TxtUserMessage.Focus();
                SetStatus($"Code loaded ({CountLines(code)} lines). Enter your question and click Analyze.");
            }
        }

        // ──────────────────────────────────────────
        // Button handlers
        // ──────────────────────────────────────────

        private void BtnSend_Click(object sender, RoutedEventArgs e) =>
            _ = SendAsync();

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            SetStatus("Cancelled.");
        }

        private void BtnClearCode_Click(object sender, RoutedEventArgs e)
        {
            TxtSelectedCode.Clear();
            SetStatus("Code cleared.");
        }

        private void BtnCopyResponse_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtResponse.Text))
            {
                Clipboard.SetText(TxtResponse.Text);
                SetStatus("Response copied to clipboard.");
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            TxtSelectedCode.Clear();
            TxtUserMessage.Clear();
            TxtResponse.Text = "Response will appear here…";
            BtnCopyResponse.IsEnabled = false;
            SetStatus("Ready");
        }

        private void TxtUserMessage_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter submits
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = SendAsync();
            }
        }

        // ──────────────────────────────────────────
        // Core LLM call
        // ──────────────────────────────────────────

        private async System.Threading.Tasks.Task SendAsync()
        {
            var userMessage = TxtUserMessage.Text?.Trim();
            if (string.IsNullOrEmpty(userMessage))
            {
                SetStatus("⚠ Please enter a question or instruction.");
                TxtUserMessage.Focus();
                return;
            }

            var selectedCode = TxtSelectedCode.Text?.Trim() ?? string.Empty;

            // Get the LLM service from the package
            var package = GetPackage();
            if (package == null)
            {
                ShowError("Could not access the CodeLens AI package. Try restarting Visual Studio.");
                return;
            }

            // Refresh options in case they changed since last call
            var options = package.GetOptions();
            if (!options.IsConfigured)
            {
                ShowError(
                    "LLM is not configured.\n\n" +
                    "Go to Tools → Options → CodeLens AI → LLM Connection " +
                    "and enter your local LLM endpoint URL.");
                return;
            }

            package.LlmService.RefreshOptions(options);

            // Set busy state
            SetBusy(true);
            TxtResponse.Text = "Analyzing…";
            BtnCopyResponse.IsEnabled = false;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                // Switch to background thread for the HTTP call
                var result = await System.Threading.Tasks.Task.Run(
                    () => package.LlmService.AnalyzeAsync(selectedCode, userMessage, token),
                    token).ConfigureAwait(true); // true = marshal back to UI thread

                if (result.Success)
                {
                    TxtResponse.Text = result.Content;
                    BtnCopyResponse.IsEnabled = true;
                    SetStatus($"✓ Done — {CountWords(result.Content)} words returned.");
                }
                else
                {
                    TxtResponse.Text = $"❌ Error\n\n{result.ErrorMessage}";
                    SetStatus("Error — see response area for details.");
                }
            }
            catch (OperationCanceledException)
            {
                TxtResponse.Text = "Request cancelled.";
                SetStatus("Cancelled.");
            }
            catch (Exception ex)
            {
                TxtResponse.Text = $"Unexpected error: {ex.Message}";
                SetStatus("Error.");
            }
            finally
            {
                SetBusy(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        // ──────────────────────────────────────────
        // UI helpers
        // ──────────────────────────────────────────

        private void SetBusy(bool busy)
        {
            BtnSend.IsEnabled = !busy;
            BtnCancel.IsEnabled = busy;
            ProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            if (busy) SetStatus("Sending to LLM…");
        }

        private void SetStatus(string message) =>
            TxtStatus.Text = message;

        private void ShowError(string message)
        {
            TxtResponse.Text = $"⚠ {message}";
            SetStatus("Configuration required.");
        }

        private static VSPackage? GetPackage()
        {
            // Retrieve the package via the VS service provider
            ThreadHelper.ThrowIfNotOnUIThread();
            return Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(VSPackage)) as VSPackage;
        }

        private static int CountLines(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Length;

        private static int CountWords(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Split(new[] { ' ', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
