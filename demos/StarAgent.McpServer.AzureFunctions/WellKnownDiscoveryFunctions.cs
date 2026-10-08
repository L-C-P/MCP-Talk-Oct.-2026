using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace StarAgent.McpServer.AzureFunctions;

/// <summary>
///     HTTP endpoints exposing MCP Server Card discovery metadata (SEP-2127).
/// </summary>
public class WellKnownDiscoveryFunctions
{
    private const string BaseUrl = "http://localhost:7071";

    [Function(nameof(GetAiCatalog))]
    public async Task<HttpResponseData> GetAiCatalog([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/ai-catalog.json")] HttpRequestData request)
    {
        var payload = new
        {
            specVersion = "1.0",
            entries = new[]
            {
                new
                {
                    identifier = "urn:air:blm2026.local:mcp:staragent-functions",
                    type = "application/mcp-server-card+json",
                    url = $"{BaseUrl}/mcp/server-card"
                }
            }
        };

        return await CreateJsonResponseAsync(request, payload, "application/ai-catalog+json");
    }

    [Function(nameof(GetServerCard))]
    public async Task<HttpResponseData> GetServerCard([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mcp/server-card")] HttpRequestData request)
    {
        var payload = new Dictionary<string, object>
        {
            ["$schema"] = "https://static.modelcontextprotocol.io/schemas/v1/server-card.schema.json",
            ["name"] = "local.blm2026/staragent-mcp-server-functions",
            ["title"] = "StarAgent",
            ["version"] = "0.1.0",
            ["description"] = "AI tour manager for concerts and artists",
            ["remotes"] = new[]
            {
                new
                {
                    type = "streamable-http",
                    url = $"{BaseUrl}/runtime/webhooks/mcp"
                }
            }
        };

        return await CreateJsonResponseAsync(request, payload, "application/mcp-server-card+json");
    }

    private static async Task<HttpResponseData> CreateJsonResponseAsync(HttpRequestData request, object payload, string contentType)
    {
        HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", contentType);
        // Discovery documents are public, read-only metadata; open CORS is acceptable.
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload));

        return response;
    }
}
