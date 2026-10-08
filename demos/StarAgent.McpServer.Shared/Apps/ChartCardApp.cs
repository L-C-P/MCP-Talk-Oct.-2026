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

    private const string EmbeddedResourceName = "StarAgent.McpServer.Shared.Apps.chart-card.html";

    private static readonly Lazy<string> _Html = new Lazy<string>(LoadHtml);

    [McpServerResource(UriTemplate = ResourceUri, Name = "chart_card", MimeType = McpApps.HtmlMimeType)]
    [Description("Interactive chart card for get_chart_position: current rank, peak, weeks on chart, and chart history.")]
    public static string GetChartCard()
    {
        return _Html.Value;
    }

    private static string LoadHtml()
    {
        using Stream stream = typeof(ChartCardApp).Assembly.GetManifestResourceStream(EmbeddedResourceName)
                              ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
