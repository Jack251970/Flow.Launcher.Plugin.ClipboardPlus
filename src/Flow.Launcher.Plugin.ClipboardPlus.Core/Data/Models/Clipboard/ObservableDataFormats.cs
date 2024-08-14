/*
 * ObservableDataFormats.cs is from https://github.com/Willy-Kimura/SharpClipboard
 * with some modification, the original source code doesn't provide a
 * license, but MIT license shown in nuget package so I copied them here
 */

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class ObservableDataFormats
{
    /// <summary>
    /// Creates a new <see cref="ObservableDataFormats"/> options class-instance.
    /// </summary>
    public ObservableDataFormats()
    {
        _all = true;
    }

    #region Fields

    private bool _all;

    #endregion

    #region Properties

    public bool All
    {
        get => _all;
        set
        {
            _all = value;

            Texts = value;
            Files = value;
            Images = value;
            Others = value;
        }
    }

    public bool Texts { get; set; } = true;
    public bool Files { get; set; } = true;
    public bool Images { get; set; } = true;
    public bool Others { get; set; } = true;

    #endregion

    #region Overrides

    public override string ToString()
    {
        return $"Texts: {Texts}; Images: {Images}; Files: {Files}; Others: {Others}";
    }

    #endregion
}
