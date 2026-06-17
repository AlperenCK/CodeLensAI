using System.Runtime.InteropServices;
using CodeLensAI.Services;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI.ToolWindows
{
    /// <summary>
    /// Tool window host for the CodeLens AI Chat panel.
    /// Receives an <see cref="ILlmHost"/> via constructor and passes it
    /// to <see cref="ChatWindowControl"/> — no global service lookup needed.
    /// </summary>
    [Guid("c3d4e5f6-a7b8-9012-cdef-123456789012")]
    public sealed class ChatWindow : ToolWindowPane
    {
        public ChatWindow() : base(null)
        {
            Caption = "CodeLens AI";
            // Control created without host; host is set by AnalyzeCommand after window is shown.
            Content = new ChatWindowControl();
        }

        /// <summary>
        /// Injects the LLM host after the window is created.
        /// Called by <see cref="Commands.AnalyzeCommand"/> before passing selected code.
        /// </summary>
        internal void SetHost(ILlmHost host)
        {
            if (Content is ChatWindowControl ctrl)
                ctrl.SetHost(host);
        }
    }
}
