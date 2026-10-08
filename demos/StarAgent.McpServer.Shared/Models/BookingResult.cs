namespace StarAgent.McpServer.Shared.Models;

/// <summary>
///     Represents the result of a venue booking attempt.
/// </summary>
public sealed record BookingResult(
    string Status,
    string Artist,
    string City,
    string Date,
    int RequestedCapacity,
    string? Venue,
    string? BookingId,
    string Message);
