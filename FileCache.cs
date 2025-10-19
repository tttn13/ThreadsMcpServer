using System.Text.Json;

public class FileCache
{
    private static readonly string CachePath = Path.Combine(
        AppContext.BaseDirectory,
        "threads_mcp_cache.json"
    );

    public static void Set(string key, string value)
    {
        var cache = GetAll();
        cache[key] = value;
        File.WriteAllText(CachePath, JsonSerializer.Serialize(cache));
    }

    public static string? Get(string key)
    {
        var cache = GetAll();
        return cache.TryGetValue(key, out var value) ? value : null;
    }

    private static Dictionary<string, string> GetAll()
    {
        if (!File.Exists(CachePath)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(CachePath)
            ) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
