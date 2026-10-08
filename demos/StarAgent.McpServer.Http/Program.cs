using ModelContextProtocol.Extensions.Apps;
using StarAgent.McpServer.Shared.Prompts;
using StarAgent.McpServer.Shared.Resources;
using StarAgent.McpServer.Shared.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Streamable HTTP is stateless by default since SDK v2 (MCP spec 2026-07-28).
builder.Services
       .AddMcpServer()
       .WithHttpTransport()
       .WithToolsFromAssembly(typeof(ChartTools).Assembly)
       .WithResourcesFromAssembly(typeof(RiderResources).Assembly)
       .WithPromptsFromAssembly(typeof(PressReleasePrompts).Assembly)
       .WithMcpApps();

WebApplication app = builder.Build();

app.MapGet("/", () => "StarAgent MCP HTTP demo server is running.");

// Pre-connection discovery (SEP-2127): AI Catalog at the domain root, Server Card next to the MCP endpoint.
app.MapGet("/.well-known/ai-catalog.json", (HttpContext context) => DiscoveryFile(context, "ai-catalog.json", "application/ai-catalog+json"));
app.MapGet("/mcp/server-card", (HttpContext context) => DiscoveryFile(context, "server-card.json", "application/mcp-server-card+json"));

app.MapMcp("/mcp");

await app.RunAsync("http://localhost:3001");

IResult DiscoveryFile(HttpContext context, string fileName, string contentType)
{
    // Discovery documents are public, read-only metadata; open CORS is acceptable.
    context.Response.Headers.AccessControlAllowOrigin = "*";

    return Results.File(Path.Combine(app.Environment.ContentRootPath, "Discovery", fileName), contentType);
}
