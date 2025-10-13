
using DotNetEnv;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using ThreadsMcpNet;

Env.Load(".env");
var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on Cloud Run's PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddMemoryCache();

// Register Redis cache service
var redisHost = builder.Configuration["Redis:Host"] ?? "localhost";
var redisPort = int.Parse(builder.Configuration["Redis:Port"] ?? "6379");
var redisUser = builder.Configuration["Redis:User"];
var redisPassword = builder.Configuration["Redis:Password"];
var redisDatabase = int.Parse(builder.Configuration["Redis:Database"] ?? "0");

builder.Services.AddSingleton<IRedisCacheService>(sp =>
    new RedisCacheService(redisHost, redisPort, redisUser, redisPassword, redisDatabase));

builder.Services.AddHttpClient<AuthService>();
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddHttpContextAccessor();

// Check if running as MCP server (stdio) or web server (http)
var isMcpMode = args.Contains("--mcp");

if (isMcpMode)
{
    // Use Host for stdio MCP server
    var hostBuilder = Host.CreateApplicationBuilder(args);

    // Suppress logging to stdout for stdio mode
    hostBuilder.Logging.ClearProviders();

    hostBuilder.Services.AddMemoryCache();

    // Register Redis cache service for MCP mode
    hostBuilder.Services.AddSingleton<IRedisCacheService>(sp =>
        new RedisCacheService(redisHost, redisPort, redisUser, redisPassword, redisDatabase));

    hostBuilder.Services.AddHttpClient<AuthService>();
    hostBuilder.Services.AddHttpClient<ApiService>();

    hostBuilder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await hostBuilder.Build().RunAsync();
    return;
}

// Web server mode for OAuth and HTTP endpoints
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Test Redis connection (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        var redisCache = app.Services.GetRequiredService<IRedisCacheService>();
        var isConnected = await redisCache.PingAsync();
        Console.WriteLine($"Redis connection status: {(isConnected ? "✓ Connected" : "✗ Failed")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis connection failed: {ex.Message}");
    }
});

// if (isConnected)
// {
//     try
//     {
//         var testKey = "startup_test_key";
//         var testValue = new { message = "Redis test", timestamp = DateTime.UtcNow };

//         var writeSuccess = await redisCache.SetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
//         Console.WriteLine($"Redis write test: {(writeSuccess ? "✓ Success" : "✗ Failed")}");

//         var readValue = await redisCache.GetAsync<object>(testKey);
//         Console.WriteLine($"Redis read test: {(readValue != null ? "✓ Success" : "✗ Failed")}");

//         var deleteSuccess = await redisCache.DeleteAsync(testKey);
//         Console.WriteLine($"Redis delete test: {(deleteSuccess ? "✓ Success" : "✗ Failed")}");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Redis operation test failed: {ex.Message}");
//     }
// }
app.MapMcp("/mcp");

app.MapGet("/init", async () =>
{
    return Results.Content("<html><body><h1>Threads MCP Server</h1></body></html>", "text/html");
});

app.MapGet("/login", async (AuthService _auth) =>
{
    var url = _auth.BuildLoginUrl();
    return Results.Redirect(url);
});

app.MapGet("/callback", async (string? code, AuthService _auth, ApiService _api) =>
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest("Code parameter is required");
    }

    var token = await _auth.ExchangeCodeForShortToken(code);

    var longToken = await _auth.ExchangeCodeForLongToken(token);

    var id = await _api.GetUserId();
    return Results.Content("<html><body><h1>Authentication successful!</h1><p>You can close this window and return to Claude.</p></body></html>", "text/html");
});

app.MapGet("/api/me", async (ApiService _api, IRedisCacheService _cache) =>
{
    var id = await _api.GetUserId();
    var userId = await _cache.GetAsync<string>("user_id");
    var token = await _cache.GetAsync<string>("long_token");
    System.Console.WriteLine($" token is {token}, userId is {userId}");

    return Results.Ok(new { id = id, token = token });
});

// The parameter input is bound from the query string by default in minimal APIs when it's a simple type like string. That's why ?input=ABC TEST works. To accept it from the body as JSON, you need to use the [FromBody] attribute or create a request model
app.MapPost("/api/post", async (string input, ApiService _api) =>
{
    var containerId = await _api.CreateTextContainer(input);
    var publishId = await _api.PublishContainer(containerId);
    return Results.Ok(publishId);
});
app.Run();

[McpServerToolType]
public static class ContentCreationTool
{
    [McpServerTool(Name = "LoginToThreads"), Description("Oauth login to Threads. User must complete authentication in browser before any other tools can be used.")]
    public static async Task<string> Authentication(McpServer thisServer, AuthService auth)
    {
        var url = auth.BuildLoginUrl();

        // Open URL in default browser
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });

        return $"⚠️ Authentication started. Oauth Url is {url}. Opening browser...\n\nYou MUST complete the authentication in your browser before I can publish posts.\n\nOnce you've completed authentication in the browser, please let me know and I'll proceed with publishing.";
    }

    [McpServerTool(Name = "CreateAndPublishPost"), Description("Create and publish the post to Threads via API")]
    public static async Task<string> CreateAndPublishPost(
    ApiService api,
    McpServer thisServer,
    IRedisCacheService redisCache,
    [Description("The content of the social media post")] string content,
    CancellationToken cancellationToken)
    {
        var userId = await redisCache.GetAsync<string>("user_id");
        var token = await redisCache.GetAsync<string>("long_token");

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return $"Please complete the authentication process in the browser first via the LoginToThreads tool";
        }

        var containerId = await api.CreateTextContainer(content);
        var mediaId = await api.PublishContainer(containerId);

        return $"✅ Successfully posted to Threads!\nPost ID: ${mediaId}`";
    }
}


public class Content
{
    [Description("The topic of the social media post")]
    public string Topic { get; set; } = "";

    [Description("The tone of the content: casual, professional, funny, or inspirational")]
    public string Tone { get; set; } = "";

    [Description("Maximum length of the post in characters")]
    public int? MaxLength { get; set; } = 50;
}

