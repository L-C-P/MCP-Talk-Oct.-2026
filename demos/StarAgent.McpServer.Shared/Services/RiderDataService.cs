using System.Text.Json;

namespace StarAgent.McpServer.Shared.Services;

/// <summary>
///     Provides backstage rider documents keyed by artist slug.
/// </summary>
public static class RiderDataService
{
    private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private static readonly Dictionary<string, string> _Riders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["van-halen"] =
            """
            {
              "artist": "Van Halen",
              "stageRequirements": [
                "Professional drum riser",
                "Two guitar amp stacks",
                "Wireless in-ear monitor system"
              ],
              "catering": [
                "Assorted sandwiches",
                "Fresh fruit trays",
                "Sparkling and still water"
              ],
              "specialRequests": [
                "Absolutely NO brown M&Ms",
                "Private backstage lounge",
                "Hot coffee available 30 minutes before showtime"
              ]
            }
            """,
        ["foo-fighters"] =
            """
            {
              "artist": "Foo Fighters",
              "stageRequirements": [
                "Dual drum kit setup",
                "FOH engineer access from 14:00",
                "Additional side-fill monitors"
              ],
              "catering": [
                "Vegetarian and vegan hot meals",
                "Energy bars",
                "Electrolyte drinks"
              ],
              "specialRequests": [
                "Quiet warm-up room",
                "Two humidifiers backstage"
              ]
            }
            """,
        ["queen"] =
            """
            {
              "artist": "Queen",
              "stageRequirements": [
                "Grand piano (tuned on show day)",
                "Four vocal mics with backup channels",
                "Rear LED wall for visual effects"
              ],
              "catering": [
                "Mediterranean buffet",
                "Still and sparkling water",
                "Fresh ginger tea"
              ],
              "specialRequests": [
                "Vintage-style dressing room mirrors",
                "Warm white stage lighting profile for encore"
              ]
            }
            """
    };

    public static string Load(string name)
    {
        string slug = NormalizeSlug(name);

        if (_Riders.TryGetValue(slug, out string? rider))
        {
            return rider;
        }

        var fallback = new
        {
            artist = string.IsNullOrWhiteSpace(name)
                ? "Unknown Artist"
                : name.Trim(),
            stageRequirements = new[]
            {
                "Standard stage setup", "Line check 60 minutes before doors"
            },
            catering = new[]
            {
                "Water", "Snacks"
            },
            specialRequests = new[]
            {
                "No dedicated rider found; using default demo rider."
            }
        };

        return JsonSerializer.Serialize(fallback, _JsonOptions);
    }

    private static string NormalizeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown-artist";
        }

        return value.Trim()
                    .ToLowerInvariant()
                    .Replace('_', '-')
                    .Replace(' ', '-');
    }
}
