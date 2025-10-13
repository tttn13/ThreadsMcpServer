using ThreadsMcpNet;

public class ApiService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly IRedisCacheService _redis;

    public ApiService(IConfiguration config, HttpClient http, IRedisCacheService redisCache)
    {
        _config = config;
        _redis = redisCache;
        _http = http;
        _http.BaseAddress = new Uri($"{_config["HOST"]}v1.0/");
    }

    public async Task<string> GetUserId()
    {
        // var accessToken = FileCache.Get("long_token");
        var accessToken = await _redis.GetAsync<string>("long_token");
        var url = $"me?fields=id,username&access_token={accessToken}";

        var response = await _http.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        var userId = result?["id"]?.ToString();

        // FileCache.Set("user_id", userId);
        await _redis.SetAsync("user_id", userId);
        return userId;
    }

    public async Task<string> CreateTextContainer(string input)
    {
        // var userId = FileCache.Get("user_id");
        // var token = FileCache.Get("long_token");
        var userId = await _redis.GetAsync<string>("user_id");
        var token = await _redis.GetAsync<string>("long_token");
        var publishUrl = $"{userId}/threads";
        var queryParams = new Dictionary<string, string>
        {
            {"media_type", "TEXT"},
            {"text", input},
            {"access_token", token}
        };
        System.Console.Error.WriteLine($"Making POST request to THreads, url is {publishUrl}");
        var response = await _http.PostAsync(publishUrl, new FormUrlEncodedContent(queryParams));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        return result?["id"]?.ToString();
    }

    public async Task<string> PublishContainer(string mediaId)
    {
        // var userId = FileCache.Get("user_id");
        // var token = FileCache.Get("long_token");
        var userId = await _redis.GetAsync<string>("user_id");
        var token = await _redis.GetAsync<string>("long_token");
        var publishUrl = $"{userId}/threads_publish";

        var queryParams = new Dictionary<string, string>
        {
            {"creation_id", mediaId},
            {"access_token", token}
        };

        var response = await _http.PostAsync(publishUrl, new FormUrlEncodedContent(queryParams));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return result?["id"]?.ToString();
    }
}