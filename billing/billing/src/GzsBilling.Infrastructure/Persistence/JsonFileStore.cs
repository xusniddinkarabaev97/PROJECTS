using System.Text.Json;

namespace GzsBilling.Infrastructure.Persistence;

/// <summary>
/// Simple JSON file-based persistence for development.
/// Saves data to /data directory on every change, loads on startup.
/// </summary>
public class JsonFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataDir;

    public JsonFileStore()
    {
        _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(_dataDir);
    }

    public async Task SaveAsync<T>(string fileName, T data)
    {
        var path = Path.Combine(_dataDir, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public T? Load<T>(string fileName) where T : class
    {
        var path = Path.Combine(_dataDir, fileName);
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SaveListAsync<T>(string fileName, List<T> list)
    {
        await SaveAsync(fileName, list);
    }

    public List<T> LoadList<T>(string fileName) where T : class
    {
        return Load<List<T>>(fileName) ?? new List<T>();
    }
}
