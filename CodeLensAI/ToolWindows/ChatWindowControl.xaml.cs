using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CodeLensAI.Services;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI.ToolWindows
{
    public partial class ChatWindowControl : UserControl
    {
        private ILlmHost _host = null!;
        private CancellationTokenSource _cts = null!;
        private string _selectedCode = string.Empty;

        public ChatWindowControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => RefreshModelName();

        public void SetHost(ILlmHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            RefreshModelName();
        }

        public void SetSelectedCode(string code)
        {
            _selectedCode = code?.Trim() ?? string.Empty;
            UpdateCodePreview();
            if (!string.IsNullOrEmpty(_selectedCode))
                TxtUserMessage.Focus();
        }

        // ── Header buttons ────────────────────────────────────────────────

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // DTE ile Tools.Options ac
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte != null)
                {
                    dte.ExecuteCommand("Tools.Options", "");
                    return;
                }
            }
            catch { }
            AppendErrorBubble("Ayarlar: Tools -> Options -> CodeLens AI -> LLM Connection");
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            _selectedCode = string.Empty;
            UpdateCodePreview();
            TxtUserMessage.Clear();
            ClearChatHistory();
        }

        private void BtnClearCode_Click(object sender, RoutedEventArgs e)
        {
            _selectedCode = string.Empty;
            UpdateCodePreview();
        }

        // ── Model pill click — cycle through profiles ─────────────────────

        private void BtnModelPill_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var opts = _host?.GetOptions();
                if (opts == null) return;

                var profiles = string.IsNullOrWhiteSpace(opts.ModelProfiles)
                    ? new string[0]
                    : opts.ModelProfiles.Split(
                        new[] { '\n', '\r', ';', ',' },
                        StringSplitOptions.RemoveEmptyEntries);

                // Build ContextMenu dynamically
                var menu = new System.Windows.Controls.ContextMenu();
                var current = opts.ModelName?.Trim() ?? string.Empty;

                // Add current model if not in profiles
                if (!string.IsNullOrWhiteSpace(current) &&
                    System.Array.FindIndex(profiles, p => p.Trim() == current) < 0)
                {
                    var item0 = new System.Windows.Controls.MenuItem
                    {
                        Header = current + " (aktif)",
                        IsChecked = true,
                        IsCheckable = false
                    };
                    menu.Items.Add(item0);
                    if (profiles.Length > 0)
                        menu.Items.Add(new System.Windows.Controls.Separator());
                }

                foreach (var profile in profiles)
                {
                    var p = profile.Trim();
                    if (string.IsNullOrEmpty(p)) continue;
                    var item = new System.Windows.Controls.MenuItem
                    {
                        Header = p,
                        IsChecked = p == current,
                        IsCheckable = false
                    };
                    var captured = p;
                    item.Click += (s, args) =>
                    {
                        opts.ModelName = captured;
                        opts.SaveSettingsToStorage();
                        RefreshModelName();
                        AppendInfoBubble("Model degistirildi: " + captured);
                    };
                    menu.Items.Add(item);
                }

                if (menu.Items.Count == 0)
                {
                    AppendInfoBubble("Model profili tanimlanmamis. Tools -> Options -> CodeLens AI -> Model Profiles");
                    return;
                }

                if (sender is System.Windows.Controls.Button btn)
                {
                    menu.PlacementTarget = btn;
                    menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    menu.IsOpen = true;
                }
            }
            catch { }
        }

        // ── Input ─────────────────────────────────────────────────────────

        private void TxtUserMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            PhUserMessage.Visibility = string.IsNullOrEmpty(TxtUserMessage.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtUserMessage_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter = send, Shift+Enter = newline
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                _ = SendAsync();
                return;
            }
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = SendAsync();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => _ = SendAsync();

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null) _cts.Cancel();
        }

        // ── Core send ─────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task SendAsync()
        {
            var message = TxtUserMessage.Text?.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (_host == null)
            {
                AppendErrorBubble("LLM host hazir degil. Paneli kapatip tekrar acin.");
                return;
            }

            var options = _host.GetOptions();
            if (!options.IsConfigured)
            {
                AppendErrorBubble("LLM yapilandirilmamis. Ayar butonuna tiklayin.");
                return;
            }

            var codeCopy = _selectedCode;
            AppendUserMessage(message!, codeCopy);
            TxtUserMessage.Clear();
            _selectedCode = string.Empty;
            UpdateCodePreview();
            WelcomePanel.Visibility = Visibility.Collapsed;

            SetBusy(true);
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var result = await _host.AnalyzeAsync(codeCopy, message!, token).ConfigureAwait(true);
                if (result.Success)
                    AppendAiBubble(result.Content);
                else
                    AppendErrorBubble(CleanErrorMessage(result.ErrorMessage ?? "Bilinmeyen hata."));
            }
            catch (OperationCanceledException)
            {
                AppendErrorBubble("Istek iptal edildi.");
            }
            catch (Exception ex)
            {
                AppendErrorBubble("Hata: " + ex.Message);
            }
            finally
            {
                SetBusy(false);
                if (_cts != null) _cts.Dispose();
                _cts = null!;
            }
        }

        // ── Error cleanup ─────────────────────────────────────────────────

        private static string CleanErrorMessage(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            var msgMatch = Regex.Match(raw, "\"message\"\\s*:\\s*\"([^\"]+)\"");
            if (msgMatch.Success)
            {
                var msg = msgMatch.Groups[1].Value;
                if (msg.Contains("<html") || msg.Contains("\\r\\n"))
                {
                    var parts = msg.Split(new string[] { "\\r\\n", "\\n", "<" }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 && parts[0].Length > 5 ? parts[0] : "Sunucu gecici olarak kullanilamiyor.";
                }
                return msg;
            }

            if (raw.Contains("<html") || raw.Contains("<!DOCTYPE"))
                return "Sunucu gecici olarak kullanilamiyor. (503)";

            var httpMatch = Regex.Match(raw, "HTTP (\\d{3})");
            if (httpMatch.Success)
            {
                var code = httpMatch.Groups[1].Value;
                if (code == "401") return "Kimlik dogrulama hatasi (401). API key kontrol edin.";
                if (code == "403") return "Erisim reddedildi (403).";
                if (code == "404") return "Endpoint bulunamadi (404). URL kontrol edin.";
                if (code == "429") return "Istek limiti asildi (429). Bekleyip tekrar deneyin.";
                if (code == "503") return "Sunucu gercici olarak kullanilamiyor (503). Bekleyip tekrar deneyin.";
                return "HTTP " + code + " hatasi.";
            }

            return raw.Length > 200 ? raw.Substring(0, 200) + "..." : raw;
        }

        // ── Bubbles ───────────────────────────────────────────────────────

        private void AppendUserMessage(string message, string code)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };

            if (!string.IsNullOrEmpty(code))
            {
                var lines = code.Split('\n');
                string preview;
                if (lines.Length > 4)
                    preview = lines[0] + "\n" + lines[1] + "\n" + lines[2] + "\n" + lines[3]
                              + "\n... (+" + (lines.Length - 4) + " satir daha)";
                else
                    preview = string.Join("\n", lines);

                var codeBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 38, 56)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 6, 8, 6),
                    MaxWidth = 280,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(16, 2, 0, 2)
                };
                codeBorder.Child = new TextBlock
                {
                    Text = preview,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 200, 255)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 80
                };
                panel.Children.Add(codeBorder);
            }

            var msgBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                CornerRadius = new CornerRadius(12, 12, 2, 12),
                Padding = new Thickness(10, 7, 10, 7),
                MaxWidth = 260,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(32, 2, 0, 8)
            };
            msgBorder.Child = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(msgBorder);
            ChatPanel.Children.Add(panel);
            ScrollToBottom();
        }

        private void AppendAiBubble(string content)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(12, 12, 12, 2),
                Padding = new Thickness(10, 7, 10, 7),
                MaxWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 32, 8),
                BorderThickness = new Thickness(1)
            };
            border.SetResourceReference(Border.BackgroundProperty, SystemColors.ControlBrushKey);
            border.SetResourceReference(Border.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);

            var tb = new TextBlock { Text = content, FontSize = 12, TextWrapping = TextWrapping.Wrap };
            tb.SetResourceReference(TextBlock.ForegroundProperty, SystemColors.WindowTextBrushKey);

            var copyBtn = new Button
            {
                Content = "Kopyala",
                FontSize = 10,
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            copyBtn.SetResourceReference(Button.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);
            copyBtn.SetResourceReference(Button.ForegroundProperty, SystemColors.GrayTextBrushKey);
            var cap = content;
            copyBtn.Click += (s, ev) => Clipboard.SetText(cap);

            var inner = new StackPanel { Orientation = Orientation.Vertical };
            inner.Children.Add(tb);
            inner.Children.Add(copyBtn);
            border.Child = inner;
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void AppendInfoBubble(string message)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 7, 10, 7),
                MaxWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 32, 8),
                BorderThickness = new Thickness(1)
            };
            border.SetResourceReference(Border.BackgroundProperty, SystemColors.ControlBrushKey);
            border.SetResourceReference(Border.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);
            border.Child = new TextBlock
            {
                Text = message,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void AppendErrorBubble(string message)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 7, 10, 7),
                MaxWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 32, 8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 80, 80)),
                Background = new SolidColorBrush(Color.FromArgb(20, 220, 80, 80))
            };
            border.Child = new TextBlock
            {
                Text = message,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
                TextWrapping = TextWrapping.Wrap
            };
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void SetBusy(bool busy)
        {
            BtnSend.IsEnabled = !busy;
            TypingRow.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshModelName()
        {
            try
            {
                var opts = _host?.GetOptions();
                TxtModelName.Text = string.IsNullOrWhiteSpace(opts?.ModelName) ? "—" : opts!.ModelName;
            }
            catch { TxtModelName.Text = "—"; }
        }

        private void UpdateCodePreview()
        {
            if (string.IsNullOrEmpty(_selectedCode))
            {
                CodePreviewBorder.Visibility = Visibility.Collapsed;
                TxtCodePreview.Text = string.Empty;
            }
            else
            {
                var lines = _selectedCode.Split('\n');
                var first = lines[0].Trim();
                var preview = lines.Length > 1 ? first + "  (" + lines.Length + " satir)" : first;
                TxtCodePreview.Text = preview.Length > 70 ? preview.Substring(0, 70) + "..." : preview;
                CodePreviewBorder.Visibility = Visibility.Visible;
            }
        }

        private void ClearChatHistory()
        {
            for (int i = ChatPanel.Children.Count - 1; i >= 0; i--)
                if (ChatPanel.Children[i] != WelcomePanel)
                    ChatPanel.Children.RemoveAt(i);
            WelcomePanel.Visibility = Visibility.Visible;
        }

        private void ScrollToBottom() => ChatScroller.ScrollToEnd();
    }
}
