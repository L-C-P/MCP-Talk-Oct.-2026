using ModelContextProtocol.Server;
using StarAgent.McpServer.Shared.Services;
using System.ComponentModel;

namespace StarAgent.McpServer.Shared.Resources;

/// <summary>
///     MCP resource endpoint exposing artist backstage riders.
/// </summary>
[McpServerResourceType]
public static class RiderResources
{
    [McpServerResource(UriTemplate = "rider://artist/{name}", Name = "artist_rider", MimeType = "application/json")]
    [Description("Returns the backstage rider for an artist. Includes stage requirements, catering, and special requests.")]
    public static string GetRider([Description("Artist slug, e.g. van-halen or foo-fighters")] string name)
    {
        return RiderDataService.Load(name);
    }
}
