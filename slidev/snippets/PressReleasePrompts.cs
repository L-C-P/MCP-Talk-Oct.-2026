using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace StarAgent.McpServer.Shared.Prompts;

/// <summary>
///     MCP prompt templates used in the StarAgent live demo.
/// </summary>
[McpServerPromptType]
public static class PressReleasePrompts
{
    [McpServerPrompt(Name = "concert_press_release")]
    [Description("Generates a dramatic concert press release.")]
    public static IEnumerable<PromptMessage> ConcertPressRelease([Description("Artist name")] string artist,
                                                                 [Description("Venue name")] string venue,
                                                                 [Description("Concert date")] string date,
                                                                 [Description("Tour name")] string tourName)
    {
        string safeArtist = NormalizeOrDefault(artist, "the featured artist");
        string safeVenue = NormalizeOrDefault(venue, "the main stage");
        string safeDate = NormalizeOrDefault(date, "TBA");
        string safeTourName = NormalizeOrDefault(tourName, "The Legend Returns Tour");

        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"Write a dramatic press release for {safeArtist} performing '{safeTourName}' at {safeVenue} on {safeDate}. The legend returns."
                }
            }
        ];
    }

    private static string NormalizeOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}
