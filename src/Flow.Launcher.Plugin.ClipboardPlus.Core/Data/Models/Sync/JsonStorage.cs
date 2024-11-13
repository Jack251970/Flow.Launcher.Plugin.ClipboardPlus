using Newtonsoft.Json;

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
        try
        {
            string json = await File.ReadAllTextAsync(_path);
            var items = JsonConvert.DeserializeObject<T>(json);
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
        var formatting = Formatting.Indented;
        string json = JsonConvert.SerializeObject(_jsonData, formatting);
        await File.WriteAllTextAsync(path, json);
    }
}
