using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CodeLensAI.ToolWindows
{
    /// <summary>
    /// Tool window host for the CodeLens AI Chat panel.
    /// The actual UI lives in <see cref="ChatWindowControl"/>.
    /// </summary>
    [Guid("c3d4e5f6-a7b8-9012-cdef-123456789012")]
    public sealed class ChatWindow : ToolWindowPane
    {
        public ChatWindow() : base(null)
        {
            Caption = "CodeLens AI";
            Content = new ChatWindowControl();
        }
    }
}
