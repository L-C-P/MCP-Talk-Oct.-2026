using StarAgent.McpServer.Shared.Models;

namespace StarAgent.McpServer.Shared.Services;

/// <summary>
///     Provides deterministic mock chart data for the live demo.
/// </summary>
public static class ChartDataService
{
    private const int MaxHistoryPoints = 20;

    /// <summary>
    ///     Charts offered by the demo UI. Any other chart name falls back to deterministic data.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownCharts = ["Billboard Hot 100", "UK Singles Chart", "Offizielle Deutsche Charts"];

    private static readonly Dictionary<string, ChartResult> _Entries = new Dictionary<string, ChartResult>(StringComparer.OrdinalIgnoreCase)
    {
        [CreateKey("Bohemian Rhapsody", "Queen", "Billboard Hot 100")] = CreateSeed(1, 1, 52, "Billboard Hot 100", "Bohemian Rhapsody", "Queen"),
        [CreateKey("Bohemian Rhapsody", "Queen", "UK Singles Chart")] = CreateSeed(1, 1, 17, "UK Singles Chart", "Bohemian Rhapsody", "Queen"),
        [CreateKey("Bohemian Rhapsody", "Queen", "Offizielle Deutsche Charts")] = CreateSeed(2, 1, 34, "Offizielle Deutsche Charts", "Bohemian Rhapsody", "Queen"),
        [CreateKey("Smells Like Teen Spirit", "Nirvana", "Billboard Hot 100")] = CreateSeed(6, 6, 38, "Billboard Hot 100", "Smells Like Teen Spirit", "Nirvana"),
        [CreateKey("Enter Sandman", "Metallica", "Billboard Hot 100")] = CreateSeed(16, 16, 20, "Billboard Hot 100", "Enter Sandman", "Metallica")
    };

    public static ChartResult Lookup(string songTitle, string artist, string chart)
    {
        string safeSongTitle = NormalizeOrDefault(songTitle, "Unknown Song");
        string safeArtist = NormalizeOrDefault(artist, "Unknown Artist");
        string safeChart = NormalizeOrDefault(chart, "Billboard Hot 100");

        if (_Entries.TryGetValue(CreateKey(safeSongTitle, safeArtist, safeChart), out ChartResult? hit))
        {
            return hit;
        }

        int hash = HashCode.Combine(safeSongTitle, safeArtist, safeChart);
        int positiveHash = hash == int.MinValue
            ? int.MaxValue
            : Math.Abs(hash);

        int rank = (positiveHash % 100) + 1;
        int peak = Math.Max(1, rank - (positiveHash % 6));
        int weeks = 4 + (positiveHash % 60);

        return new ChartResult(
            rank,
            peak,
            weeks,
            safeChart,
            safeSongTitle,
            safeArtist,
            true,
            "StarAgent deterministic fallback",
            BuildHistory(rank, peak, weeks));
    }

    private static ChartResult CreateSeed(int rank, int peak, int weeks, string chart, string songTitle, string artist)
    {
        return new ChartResult(
            rank,
            peak,
            weeks,
            chart,
            songTitle,
            artist,
            false,
            "StarAgent demo seed",
            BuildHistory(rank, peak, weeks));
    }

    /// <summary>
    ///     Builds a plausible chart run: the song enters low, climbs to its peak, then settles on its current rank.
    /// </summary>
    private static int[] BuildHistory(int rank, int peak, int weeks)
    {
        int points = Math.Clamp(weeks, 2, MaxHistoryPoints);
        int entry = Math.Min(100, peak + 35);
        var history = new int[points];

        for (int i = 0; i < points; i++)
        {
            double progress = (double)i / (points - 1);
            double position = progress <= 0.5
                ? entry + ((peak - entry) * (progress / 0.5))
                : peak + ((rank - peak) * ((progress - 0.5) / 0.5));

            history[i] = Math.Clamp((int)Math.Round(position), 1, 100);
        }

        return history;
    }

    private static string CreateKey(string songTitle, string artist, string chart)
    {
        return $"{songTitle.Trim().ToLowerInvariant()}|{artist.Trim().ToLowerInvariant()}|{chart.Trim().ToLowerInvariant()}";
    }

    private static string NormalizeOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}
