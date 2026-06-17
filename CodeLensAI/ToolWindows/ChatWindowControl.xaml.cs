using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodeLensAI.Services;

namespace CodeLensAI.ToolWindows
{
    /// <summary>
    /// Code-behind for the CodeLens AI chat panel.
    /// Depends on <see cref="ILlmHost"/> injected via <see cref="SetHost"/>;
    /// no global service lookup, fully testable.
    /// </summary>
    public partial class ChatWindowControl : UserControl
    {
        private ILlmHost? _host;
        private CancellationTokenSource? _cts;

        public ChatWindowControl()
        {
            InitializeComponent();
        }

        // ──────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────

        /// <summary>Injects the LLM host. Must be called before the user hits Analyze.</summary>
        public void SetHost(ILlmHost host) =>
            _host = host ?? throw new ArgumentNullException(nameof(host));

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
            TxtSelectedCode.Text = string.Empty;
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
            TxtSelectedCode.Text = string.Empty;
            TxtUserMessage.Text = string.Empty;
            TxtResponse.Text = string.Empty;
            BtnCopyResponse.IsEnabled = false;
            PhSelectedCode.Visibility = System.Windows.Visibility.Visible;
            PhUserMessage.Visibility  = System.Windows.Visibility.Visible;
            PhResponse.Visibility     = System.Windows.Visibility.Visible;
            SetStatus("Ready");
        }

        private void TxtUserMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = SendAsync();
            }
        }

        // ──────────────────────────────────────────
        // Core LLM call
        // ──────────────────────────────────────────


        private void TxtSelectedCode_TextChanged(object sender, TextChangedEventArgs e) =>
            PhSelectedCode.Visibility = string.IsNullOrEmpty(TxtSelectedCode.Text)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        private void TxtUserMessage_TextChanged(object sender, TextChangedEventArgs e) =>
            PhUserMessage.Visibility = string.IsNullOrEmpty(TxtUserMessage.Text)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        private async System.Threading.Tasks.Task SendAsync()
        {
            var userMessage = TxtUserMessage.Text?.Trim();
            if (string.IsNullOrEmpty(userMessage))
            {
                SetStatus("⚠ Please enter a question or instruction.");
                TxtUserMessage.Focus();
                return;
            }

            if (_host == null)
            {
                ShowError("LLM host not initialized. Try closing and reopening this panel.");
                return;
            }

            var options = _host.GetOptions();
            if (!options.IsConfigured)
            {
                ShowError(
                    "LLM is not configured.\n\n" +
                    "Go to Tools → Options → CodeLens AI → LLM Connection " +
                    "and enter your local LLM endpoint URL.");
                return;
            }

            var selectedCode = TxtSelectedCode.Text?.Trim() ?? string.Empty;

            SetBusy(true);
            TxtResponse.Text = string.Empty;
            BtnCopyResponse.IsEnabled = false;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var result = await _host
                    .AnalyzeAsync(selectedCode, userMessage!, token)
                    .ConfigureAwait(true); // marshal back to UI thread

                if (result.Success)
                {
                    TxtResponse.Text = result.Content;
                    PhResponse.Visibility = System.Windows.Visibility.Collapsed;
                    BtnCopyResponse.IsEnabled = true;
                    SetStatus($"✓ Done — {CountWords(result.Content)} words returned.");
                }
                else
                {
                    TxtResponse.Text = $"❌ Error\n\n{result.ErrorMessage}";
                    PhResponse.Visibility = System.Windows.Visibility.Collapsed;
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

        private static int CountLines(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Length;

        private static int CountWords(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Split(
                new[] { ' ', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
