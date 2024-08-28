using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Helpers;

public class RichTextBoxHelper : DependencyObject
{
    private static readonly HashSet<Thread> _recursionProtection = new();

    public static string GetDocumentRtf(DependencyObject obj)
    {
        return (string)obj.GetValue(DocumentRtfProperty);
    }

    public static void SetDocumentRtf(DependencyObject obj, string value)
    {
        _recursionProtection.Add(Thread.CurrentThread);
        obj.SetValue(DocumentRtfProperty, value);
        _recursionProtection.Remove(Thread.CurrentThread);
    }

    public static readonly DependencyProperty DocumentRtfProperty = DependencyProperty.RegisterAttached(
        "DocumentRtf",
        typeof(string),
        typeof(RichTextBoxHelper),
        new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (obj, e) => {
                if (_recursionProtection.Contains(Thread.CurrentThread))
                {
                    return;
                }

                var richTextBox = (RichTextBox)obj;
                try
                {
                    richTextBox.SetRichText(GetDocumentRtf(richTextBox));
                }
                catch (Exception)
                {
                    richTextBox.Document = new FlowDocument();
                }

                richTextBox.TextChanged += (obj2, e2) =>
                {
                    if (obj2 is RichTextBox richTextBox2)
                    {
                        SetDocumentRtf(richTextBox, richTextBox2.GetRichText());
                    }
                };
            }
        )
    );
}

