
using ThreadsMcpNet;

public class AuthService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly IRedisCacheService _redis;

    public AuthService(IConfiguration config, HttpClient http, IRedisCacheService redisCache)
    {
        _config = config;
        _redis = redisCache;
        _http = http;
        _http.BaseAddress = new Uri(_config["HOST"]);
    }

    public string BuildLoginUrl()
    {
        var redirectUri = _config["REDIRECT_URI"];
        var clientId = _config["APP_ID"];
        var scopes = "threads_basic,threads_content_publish";
       
        var authUrl = $"https://threads.net/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope={scopes}&response_type=code";
        return authUrl;
    }

    public async Task<string> ExchangeCodeForShortToken(string authCode)
    {
        var clientId = _config["APP_ID"];
        var clientSecret = _config["APP_SECRET"];
        var redirectUri = _config["REDIRECT_URI"];

        var queryParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "scope", "threads_basic,threads_content_publish"},
            { "code", authCode },
            { "grant_type", "authorization_code" }
        };

        var response = await _http.PostAsync("oauth/access_token", new FormUrlEncodedContent(queryParams));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return result?["access_token"]?.ToString();
    }

    public async Task<string> ExchangeCodeForLongToken(string token)
    {

        var clientSecret = _config["APP_SECRET"];
        var response = await _http.GetAsync($"access_token?grant_type=th_exchange_token&client_secret={clientSecret}&access_token={token}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var longToken = result?["access_token"]?.ToString();
        // FileCache.Set("long_token", longToken);
        await _redis.SetAsync("long_token", longToken);
        return longToken;
    }

}