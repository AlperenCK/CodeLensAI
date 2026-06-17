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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RefreshModelName();
        }

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
                // En güvenilir yol: DTE üzerinden Tools.Options komutunu çalıştır
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                dte?.ExecuteCommand("Tools.Options", "CodeLens AI");
            }
            catch
            {
                // DTE çalışmazsa bilgi balonu göster
                AppendErrorBubble("Ayarlar için: Tools → Options → CodeLens AI → LLM Connection");
            }
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

        // ── Input handlers ────────────────────────────────────────────────

        private void TxtUserMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            PhUserMessage.Visibility = string.IsNullOrEmpty(TxtUserMessage.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtUserMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = SendAsync();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) =>
            _ = SendAsync();

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
                AppendErrorBubble("LLM host hazır değil. Paneli kapatıp tekrar açın.");
                return;
            }

            var options = _host.GetOptions();
            if (!options.IsConfigured)
            {
                AppendErrorBubble("LLM yapılandırılmamış. ⚙ butonuna tıklayarak ayarları yapın.");
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
                var result = await _host
                    .AnalyzeAsync(codeCopy, message!, token)
                    .ConfigureAwait(true);

                if (result.Success)
                    AppendAiBubble(result.Content);
                else
                    AppendErrorBubble(CleanErrorMessage(result.ErrorMessage ?? "Bilinmeyen hata."));
            }
            catch (OperationCanceledException)
            {
                AppendErrorBubble("İstek iptal edildi.");
            }
            catch (Exception ex)
            {
                AppendErrorBubble($"Hata: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
                if (_cts != null) _cts.Dispose();
                _cts = null!;
            }
        }

        // ── Error message cleanup ─────────────────────────────────────────

        /// <summary>
        /// LLM hata mesajlarından HTML ve gürültüyü temizler.
        /// 503 gibi durumlarda LiteLLM HTML sayfası döndürebilir.
        /// </summary>
        private static string CleanErrorMessage(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // JSON içindeki "message" alanını çıkar
            var msgMatch = Regex.Match(raw, ""message"\s*:\s*"([^"]+)"");
            if (msgMatch.Success)
            {
                var msg = msgMatch.Groups[1].Value;
                // HTML içeriyorsa sadece ilk anlamlı satırı al
                if (msg.Contains("<html") || msg.Contains("\r\n"))
                {
                    var firstLine = msg.Split(new[] { "\r\n", "\n", "<" },
                        StringSplitOptions.RemoveEmptyEntries)[0];
                    return firstLine.Length > 5 ? firstLine : "Sunucu geçici olarak kullanılamıyor.";
                }
                return msg;
            }

            // Düz HTML gelirse sil
            if (raw.Contains("<html") || raw.Contains("<!DOCTYPE"))
                return "Sunucu geçici olarak kullanılamıyor. (503)";

            // HTTP status kodunu öne çıkar
            var httpMatch = Regex.Match(raw, "HTTP (\d{3})");
            if (httpMatch.Success)
            {
                var code = httpMatch.Groups[1].Value;
                return code switch
                {
                    "401" => "Kimlik doğrulama hatası (401). API key'inizi kontrol edin.",
                    "403" => "Erişim reddedildi (403). API key izinlerini kontrol edin.",
                    "404" => "Endpoint bulunamadı (404). URL'yi kontrol edin.",
                    "429" => "İstek limiti aşıldı (429). Biraz bekleyip tekrar deneyin.",
                    "500" => "Sunucu hatası (500). LLM sunucusunu kontrol edin.",
                    "503" => "Sunucu geçici olarak kullanılamıyor (503). Biraz bekleyip tekrar deneyin.",
                    _ => $"HTTP {code} hatası. LLM sunucusunu kontrol edin."
                };
            }

            // 200 karakter üzeriyse kes
            return raw.Length > 200 ? raw.Substring(0, 200) + "…" : raw;
        }

        // ── Chat bubble builders ──────────────────────────────────────────

        private void AppendUserMessage(string message, string code)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };

            // Kod önizleme balonu
            if (!string.IsNullOrEmpty(code))
            {
                var lines = code.Split('\n');
                var previewLines = lines.Length > 4
                    ? new string[] { lines[0], lines[1], lines[2], lines[3], $"… (+{lines.Length - 4} satır)" }
                    : lines;
                var preview = string.Join("\n", previewLines);

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

            // Mesaj balonu
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

            var tb = new TextBlock
            {
                Text = content,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
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

            var captured = content;
            copyBtn.Click += (s, e) => Clipboard.SetText(captured);

            var inner = new StackPanel { Orientation = Orientation.Vertical };
            inner.Children.Add(tb);
            inner.Children.Add(copyBtn);
            border.Child = inner;

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
                TxtModelName.Text = string.IsNullOrWhiteSpace(opts?.ModelName)
                    ? "—" : opts!.ModelName;
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
                var preview = lines.Length > 1
                    ? $"{lines[0].Trim()} … ({lines.Length} satır)"
                    : lines[0].Trim();
                TxtCodePreview.Text = preview.Length > 70
                    ? preview.Substring(0, 70) + "…"
                    : preview;
                CodePreviewBorder.Visibility = Visibility.Visible;
            }
        }

        private void ClearChatHistory()
        {
            for (int i = ChatPanel.Children.Count - 1; i >= 0; i--)
            {
                if (ChatPanel.Children[i] != WelcomePanel)
                    ChatPanel.Children.RemoveAt(i);
            }
            WelcomePanel.Visibility = Visibility.Visible;
        }

        private void ScrollToBottom()
        {
            ChatScroller.ScrollToEnd();
        }
    }
}
