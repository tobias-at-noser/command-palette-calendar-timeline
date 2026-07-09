using System.Text.RegularExpressions;

namespace CalendarTimeline.Core;

public static partial class TeamsLinkDetector
{
    public static string? TryFind(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = TeamsUrlRegex().Match(text);
        return match.Success ? match.Value.TrimEnd('.', ',', ';', ')') : null;
    }

    [GeneratedRegex("https://(?:teams\\.microsoft\\.com|aka\\.ms/JoinTeamsMeeting)[^\\s<>\"']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TeamsUrlRegex();
}
