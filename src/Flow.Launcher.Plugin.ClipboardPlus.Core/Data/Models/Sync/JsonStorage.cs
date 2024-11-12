using System.Text.Json;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class JsonStorage<T> where T : new()
{
    protected T _jsonData = default!;

    protected readonly string _path;

    public JsonStorage(string path)
    {
        _path = path;
    }

    protected async Task<bool> ReadAsync()
    {
        if (!File.Exists(_path))
        {
            return false;
        }
        await using FileStream openStream = File.OpenRead(_path);
        try
        {
            var items = await JsonSerializer.DeserializeAsync<T>(openStream);
            if (items != null)
            {
                _jsonData = items;
                return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }
        return false;
    }

    protected async Task WriteAsync()
    {
        await WriteAsync(_path);
    }

    protected async Task WriteAsync(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream openStream = File.Create(path);
        await JsonSerializer.SerializeAsync(openStream, _jsonData, options);
    }
}
