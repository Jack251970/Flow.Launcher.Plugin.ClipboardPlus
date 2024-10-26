using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class ClipboardDataPair : IDisposable
{
    public required ClipboardData ClipboardData { get; set; }

    public required Lazy<UserControl> PreviewPanel { get; set; }

    public void Dispose()
    {
        if (PreviewPanel.IsValueCreated)
        {
            if (PreviewPanel.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        PreviewPanel = null!;
        ClipboardData.Dispose();
    }
}
