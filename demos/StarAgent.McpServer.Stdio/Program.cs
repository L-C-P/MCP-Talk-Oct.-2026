using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Extensions.Apps;

using StarAgent.McpServer.Shared.Prompts;
using StarAgent.McpServer.Shared.Resources;
using StarAgent.McpServer.Shared.Tools;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
// Add MCP services: stdio transport and discovery from attributed classes in the shared demo assembly.
builder.Services
       .AddMcpServer()
       .WithStdioServerTransport()
       .WithToolsFromAssembly(typeof(ChartTools).Assembly)
       .WithResourcesFromAssembly(typeof(RiderResources).Assembly)
       .WithPromptsFromAssembly(typeof(PressReleasePrompts).Assembly)
       .WithMcpApps();

await builder.Build().RunAsync();
