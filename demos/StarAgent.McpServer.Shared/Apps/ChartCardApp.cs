using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace StarAgent.McpServer.Shared.Apps;

/// <summary>
///     MCP Apps UI resource that renders a chart position as an interactive chart card.
/// </summary>
[McpServerResourceType]
public static class ChartCardApp
{
    public const string ResourceUri = "ui://staragent/chart-card.html";

    private const string HtmlResourceName = "StarAgent.McpServer.Shared.Apps.chart-card.html";

    private const string SdkResourceName = "StarAgent.McpServer.Shared.Apps.mcp-apps-sdk.js";

    // Placeholder in chart-card.html that is replaced by the MCP Apps SDK, so the resource stays a single HTML file.
    private const string SdkPlaceholder = "/* @mcp-apps-sdk */";

    private static readonly Lazy<string> _Html = new Lazy<string>(LoadHtml);

    [McpServerResource(UriTemplate = ResourceUri, Name = "chart_card", MimeType = McpApps.HtmlMimeType)]
    [Description("Interactive chart card for get_chart_position: current rank, peak, weeks on chart, and chart history.")]
    public static string GetChartCard()
    {
        return _Html.Value;
    }

    private static string LoadHtml()
    {
        return ReadResource(HtmlResourceName).Replace(SdkPlaceholder, ReadResource(SdkResourceName), StringComparison.Ordinal);
    }

    private static string ReadResource(string name)
    {
        using Stream stream = typeof(ChartCardApp).Assembly.GetManifestResourceStream(name)
                              ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
