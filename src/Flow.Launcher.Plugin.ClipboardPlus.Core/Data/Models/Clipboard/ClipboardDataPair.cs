using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class ClipboardDataPair : IDisposable
{
    public required ClipboardData ClipboardData { get; set; }

    public required Lazy<UserControl> PreviewPanel { get; set; }

    public void TogglePinned()
    {
        var originalPinned = ClipboardData.Pinned;
        var newClipboardData = ClipboardData.Clone(!originalPinned);
        ClipboardData = newClipboardData;
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
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
            _disposed = true;
        }
    }
}
