using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using StarAgent.McpServer.Shared.Services;

namespace StarAgent.McpServer.AzureFunctions;

/// <summary>
///     MCP resource triggers hosted by Azure Functions.
/// </summary>
public class McpResourceFunctions
{
    [Function(nameof(GetArtistRider))]
    public string GetArtistRider([McpResourceTrigger("rider://artist/{name}", "artist_rider", Description = "Returns the backstage rider for an artist.", MimeType = "application/json")] ResourceInvocationContext context)
    {
        string slug = ExtractArtistSlug(context.Uri);
        return RiderDataService.Load(slug);
    }

    private static string ExtractArtistSlug(string? uriValue)
    {
        if (string.IsNullOrWhiteSpace(uriValue))
        {
            return "unknown-artist";
        }

        if (!Uri.TryCreate(uriValue, UriKind.Absolute, out Uri? uri))
        {
            return "unknown-artist";
        }

        string[] segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length != 0
            ? segments[^1]
            : "unknown-artist";
    }
}
