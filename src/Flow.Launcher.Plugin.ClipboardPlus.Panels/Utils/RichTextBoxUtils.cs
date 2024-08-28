using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Utils;

public static class RichTextBoxUtils
{
    public static void SetUnicodeText(this RichTextBox richTextBox, string uniText)
    {
        // Clear the existing contents
        richTextBox.Document.Blocks.Clear();

        // Load the Unicode text into the RichTextBox
        richTextBox.AppendText(uniText);
    }

    public static string GetUnicodeText(this RichTextBox richTextBox)
    {
        // Convert the RichTextBox contents to a string
        return new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
    }

    public static void SetRichText(this RichTextBox richTextBox, string rtfText)
    {
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

    public static string GetRichText(this RichTextBox richTextBox)
    {
        // Save the RichTextBox contents to a MemoryStream
        using var stream = new MemoryStream();
        richTextBox.Selection.Save(stream, DataFormats.Rtf);

        // Convert the MemoryStream to a string
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
