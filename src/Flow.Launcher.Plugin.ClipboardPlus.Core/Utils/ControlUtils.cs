using System.Text;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static class ControlUtils
{
    public static void SetUnicodeText(this RichTextBox richTextBox, string uniText)
    {
        if (string.IsNullOrEmpty(uniText))
        {
            return;
        }

        // Clear the existing contents
        richTextBox.Document.Blocks.Clear();
        // Load the Unicode text into the RichTextBox
        richTextBox.AppendText(uniText);
    }

    public static void SetRichText(this RichTextBox richTextBox, string rtfText)
    {
        if (string.IsNullOrEmpty(rtfText))
        {
            return;
        }

        using var stream = new MemoryStream();
        // Convert the RTF string to a byte array and write it to the MemoryStream
        byte[] rtfBytes = Encoding.UTF8.GetBytes(rtfText);
        stream.Write(rtfBytes, 0, rtfBytes.Length);
        stream.Position = 0;
        // Clear the existing contents
        richTextBox.Document.Blocks.Clear();
        // Load the RTF into the RichTextBox
        richTextBox.Selection.Load(stream, DataFormats.Rtf);
    }
}
