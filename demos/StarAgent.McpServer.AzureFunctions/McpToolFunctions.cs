using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using StarAgent.McpServer.Shared.Models;
using StarAgent.McpServer.Shared.Services;

namespace StarAgent.McpServer.AzureFunctions;

/// <summary>
///     MCP tool triggers hosted by Azure Functions.
/// </summary>
public class McpToolFunctions
{
    [Function(nameof(GetChartPosition))]
    public ChartResult GetChartPosition([McpToolTrigger("get_chart_position", "Returns the chart position of a song on a given chart.")] ToolInvocationContext context,
                                        [McpToolProperty("songTitle", "Song title", true)] string songTitle,
                                        [McpToolProperty("artist", "Artist name", true)] string artist,
                                        [McpToolProperty("chart", "Chart name")] string? chart)
    {
        string resolvedChart = string.IsNullOrWhiteSpace(chart)
            ? "Billboard Hot 100"
            : chart;

        return ChartDataService.Lookup(songTitle, artist, resolvedChart);
    }

    [Function(nameof(BookVenue))]
    public BookingResult BookVenue([McpToolTrigger("book_venue", "Books a concert venue for an artist on a given date.")] ToolInvocationContext context,
                                   [McpToolProperty("artist", "Artist name", true)] string artist,
                                   [McpToolProperty("city", "City", true)] string city,
                                   [McpToolProperty("date", "Date (yyyy-MM-dd)", true)] string date,
                                   [McpToolProperty("capacity", "Required capacity", true)] int capacity)
    {
        return VenueService.Book(artist, city, date, capacity);
    }
}
