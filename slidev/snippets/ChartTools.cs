using ModelContextProtocol.Server;
using StarAgent.McpServer.Shared.Models;
using StarAgent.McpServer.Shared.Services;
using System.ComponentModel;

namespace StarAgent.McpServer.Shared.Tools;

/// <summary>
///     MCP tool endpoints for chart lookup and venue booking.
/// </summary>
[McpServerToolType]
public static class ChartTools
{
    [McpServerTool(Name = "get_chart_position")]
    [Description("Returns the chart position of a song on a given chart.")]
    public static ChartResult GetChartPosition([Description("Song title")] string songTitle,
                                               [Description("Artist name")] string artist,
                                               [Description("Chart name")] string chart = "Billboard Hot 100")
    {
        return ChartDataService.Lookup(songTitle, artist, chart);
    }

    [McpServerTool(Name = "book_venue")]
    [Description("Books a concert venue for an artist on a given date.")]
    public static BookingResult BookVenue([Description("Artist name")] string artist,
                                          [Description("City")] string city,
                                          [Description("Date (yyyy-MM-dd)")] string date,
                                          [Description("Required capacity")] int capacity)
    {
        return VenueService.Book(artist, city, date, capacity);
    }
}
