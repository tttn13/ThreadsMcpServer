# ThreadsMcpNet

A .NET 8 MCP (Model Context Protocol) server for publishing content to Threads via the Threads API.

## Features

- **MCP Server Integration**: Expose Threads API functionality as MCP tools for AI assistants
- **OAuth Authentication**: Secure login flow for Threads
- **Content Publishing**: Create and publish text posts to Threads
- **Dual Mode Operation**:
  - **Stdio Mode**: Run as an MCP server for Claude Desktop or other MCP clients
  - **HTTP Mode**: Run as a web server with REST endpoints
- **Redis Caching**: Store authentication tokens and session data
- **Cloud-Ready**: Configured for deployment to Google Cloud Run

## Prerequisites

- .NET 8.0 SDK
- Redis server (local or remote)
- Threads API credentials (App ID, App Secret)

## Configuration

1. Copy `.env.example` to `.env`:
```bash
cp .env.example .env
```

2. Update the `.env` file with your credentials:
```env
HOST=https://graph.threads.net/
APP_ID=your_threads_app_id
APP_SECRET=your_threads_app_secret
REDIRECT_URI=your_redirect_url
Redis__Host=your_redis_host
Redis__User=your_redis_user
Redis__Password=your_redis_password
Redis__Database=0
```

## Installation

```bash
dotnet restore
dotnet build
```

## Usage

### Run as MCP Server (Stdio Mode)

For use with Claude Desktop or other MCP clients:

```bash
dotnet run -- --mcp
```

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "threads": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/ThreadsMcpNet", "--", "--mcp"]
    }
  }
}
```

### Run as Web Server (HTTP Mode)

```bash
dotnet run
```

The server will start on port 8080 (or the port specified by the `PORT` environment variable).

## Available Tools

When running as an MCP server, the following tools are exposed:

### `LoginToThreads`
Initiates OAuth login to Threads. Opens the authentication URL in the user's default browser. Must be completed before publishing posts.

### `CreateAndPublishPost`
Creates and publishes a text post to Threads.

**Parameters:**
- `content` (string): The content of the post to publish

## API Endpoints

When running in HTTP mode, the following endpoints are available:

- `GET /init` - Server status page
- `GET /login` - Initiate OAuth login
- `GET /callback` - OAuth callback handler
- `GET /api/me` - Get current user info
- `POST /api/post` - Create and publish a post (query param: `input`)
- `/mcp` - MCP protocol endpoint

## Project Structure

- `Program.cs` - Main application entry point and configuration
- `AuthService.cs` - OAuth authentication logic
- `ApiService.cs` - Threads API integration
- `RedisCache.cs` - Redis caching implementation
- `FileCache.cs` - File-based caching fallback

## Dependencies

- **ModelContextProtocol** - MCP server implementation
- **ModelContextProtocol.AspNetCore** - ASP.NET Core integration
- **StackExchange.Redis** - Redis client
- **DotNetEnv** - Environment variable loading
- **System.Text.Json** - JSON serialization

## Deployment

The project is configured for Google Cloud Run deployment. See `buid-push.sh` for build and deployment scripts.

## License

MIT
