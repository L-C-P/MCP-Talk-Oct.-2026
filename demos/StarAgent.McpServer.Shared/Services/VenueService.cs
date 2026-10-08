using StarAgent.McpServer.Shared.Models;

namespace StarAgent.McpServer.Shared.Services;

/// <summary>
///     Provides deterministic venue booking behavior for demo scenarios.
/// </summary>
public static class VenueService
{
    private static readonly Dictionary<string, VenueOption[]> _VenuesByCity = new Dictionary<string, VenueOption[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["London"] =
        [
            new VenueOption("Wembley Stadium", 90000),
            new VenueOption("The O2", 20000)
        ],
        ["Berlin"] =
        [
            new VenueOption("Uber Arena", 17000),
            new VenueOption("Waldbuhne", 22000)
        ],
        ["Munich"] =
        [
            new VenueOption("Olympiastadion", 69000),
            new VenueOption("Zenith", 6000)
        ],
        ["Hamburg"] =
        [
            new VenueOption("Barclays Arena", 16000),
            new VenueOption("Docks", 1500)
        ]
    };

    public static BookingResult Book(string artist, string city, string date, int capacity)
    {
        string safeArtist = NormalizeOrDefault(artist, "Unknown Artist");
        string safeCity = NormalizeOrDefault(city, "Unknown City");

        if (!DateOnly.TryParse(date, out DateOnly parsedDate))
        {
            return new BookingResult(
                "rejected",
                safeArtist,
                safeCity,
                date,
                capacity,
                null,
                null,
                "Date must use the yyyy-MM-dd format.");
        }

        if (capacity <= 0)
        {
            return new BookingResult(
                "rejected",
                safeArtist,
                safeCity,
                parsedDate.ToString("yyyy-MM-dd"),
                capacity,
                null,
                null,
                "Capacity must be greater than zero.");
        }

        if (!_VenuesByCity.TryGetValue(safeCity, out VenueOption[]? options))
        {
            return new BookingResult(
                "waitlist",
                safeArtist,
                safeCity,
                parsedDate.ToString("yyyy-MM-dd"),
                capacity,
                null,
                null,
                "No venues registered for this city in the StarAgent demo catalog.");
        }

        VenueOption? match = options
                            .OrderBy(v => v.Capacity)
                            .FirstOrDefault(v => v.Capacity >= capacity);

        if (match is null)
        {
            return new BookingResult(
                "waitlist",
                safeArtist,
                safeCity,
                parsedDate.ToString("yyyy-MM-dd"),
                capacity,
                null,
                null,
                "No venue in this city can satisfy the requested capacity.");
        }

        string bookingId = BuildBookingId(safeArtist, safeCity, parsedDate, capacity);

        return new BookingResult(
            "confirmed",
            safeArtist,
            safeCity,
            parsedDate.ToString("yyyy-MM-dd"),
            capacity,
            match.Name,
            bookingId,
            $"Venue '{match.Name}' reserved successfully.");
    }

    private static string BuildBookingId(string artist, string city, DateOnly date, int capacity)
    {
        int hash = HashCode.Combine(artist, city, date, capacity);
        int positiveHash = hash == int.MinValue
            ? int.MaxValue
            : Math.Abs(hash);

        return $"BKG-{date:yyyyMMdd}-{positiveHash % 10000:0000}";
    }

    private static string NormalizeOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private sealed record VenueOption(string Name, int Capacity);
}
