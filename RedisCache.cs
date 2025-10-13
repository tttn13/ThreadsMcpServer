using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ThreadsMcpNet;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<long> IncrementAsync(string key, long value = 1);
    Task<long> DecrementAsync(string key, long value = 1);
    Task<bool> SetAddAsync<T>(string key, T value);
    Task<T[]> SetMembersAsync<T>(string key);
    Task<bool> SetRemoveAsync<T>(string key, T value);
    Task<bool> PingAsync();
    bool IsConnected { get; }
}

public class RedisCacheService : IRedisCacheService, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsConnected => _redis?.IsConnected ?? false;

    public RedisCacheService(string host, int port, string? user = null, string? password = null, int database = 0)
    {
        var configOptions = new ConfigurationOptions
        {
            EndPoints = { { host, port } },
            User = user,
            Password = password,
            AbortOnConnectFail = false
        };

        _redis = ConnectionMultiplexer.Connect(configOptions);
        _database = _redis.GetDatabase(database);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public RedisCacheService(string connectionString, int database = 0)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase(database);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
            return default;

        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        return await _database.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task<bool> DeleteAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await _database.KeyExistsAsync(key);
    }

    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await _database.StringIncrementAsync(key, value);
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await _database.StringDecrementAsync(key, value);
    }

    public async Task<bool> SetAddAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        return await _database.SetAddAsync(key, serializedValue);
    }

    public async Task<T[]> SetMembersAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        var values = await _database.SetMembersAsync(key);
        var result = new T[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions)!;
        }

        return result;
    }

    public async Task<bool> SetRemoveAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        return await _database.SetRemoveAsync(key, serializedValue);
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
