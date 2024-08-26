using System.Text;
using System.Windows.Documents;
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

        // Create a new Paragraph and add the Unicode text to it
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run(uniText));

        // Add the Paragraph to the RichTextBox
        richTextBox.Document.Blocks.Add(paragraph);
    }

    public static void SetRichText(this RichTextBox richTextBox, string rtfText)
    {
        if (string.IsNullOrEmpty(rtfText))
        {
            return;
        }

        // Convert the RTF string to a byte array and write it to the MemoryStream
        using var stream = new MemoryStream();
        byte[] rtfBytes = Encoding.UTF8.GetBytes(rtfText);
        stream.Write(rtfBytes, 0, rtfBytes.Length);
        stream.Position = 0;

        // Clear the existing contents
        richTextBox.Document.Blocks.Clear();

        // Load the RTF into the RichTextBox
        richTextBox.Selection.Load(stream, DataFormats.Rtf);
    }
}
