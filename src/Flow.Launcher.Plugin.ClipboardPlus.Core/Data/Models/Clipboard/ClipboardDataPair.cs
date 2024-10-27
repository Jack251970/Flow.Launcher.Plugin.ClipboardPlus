using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class ClipboardDataPair : IDisposable
{
    public required ClipboardData ClipboardData { get; set; }

    public required Lazy<UserControl> PreviewPanel { get; set; }

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
        }
    }
}
