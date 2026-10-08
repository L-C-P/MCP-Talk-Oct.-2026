using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace StarAgent.McpServer.AzureFunctions;

/// <summary>
///     MCP prompt triggers hosted by Azure Functions.
/// </summary>
public class McpPromptFunctions
{
    [Function(nameof(GetConcertPressReleasePrompt))]
    public string GetConcertPressReleasePrompt(
        [McpPromptTrigger("concert_press_release", Description = "Generates a dramatic concert press release.")] PromptInvocationContext context,
        [McpPromptArgument("artist", "Artist name", true)] string artist = "the featured artist",
        [McpPromptArgument("venue", "Venue name", true)] string venue = "the main stage",
        [McpPromptArgument("date", "Concert date", true)] string date = "TBA",
        [McpPromptArgument("tourName", "Tour name", true)] string tourName = "The Legend Returns Tour")
    {
        return $"Write a dramatic press release for {artist} performing '{tourName}' at {venue} on {date}. The legend returns.";
    }
}
