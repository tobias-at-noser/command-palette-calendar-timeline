using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TeamsLinkDetectorTests
{
    [Fact]
    public void TryFindReturnsNullForEmptyText()
    {
        Assert.Null(TeamsLinkDetector.TryFind(null));
        Assert.Null(TeamsLinkDetector.TryFind(""));
        Assert.Null(TeamsLinkDetector.TryFind("No meeting link here"));
    }

    [Theory]
    [InlineData("Join https://teams.microsoft.com/l/meetup-join/abc now", "https://teams.microsoft.com/l/meetup-join/abc")]
    [InlineData("Link: https://aka.ms/JoinTeamsMeeting?id=123.", "https://aka.ms/JoinTeamsMeeting?id=123")]
    public void TryFindExtractsTeamsUrl(string text, string expected)
    {
        Assert.Equal(expected, TeamsLinkDetector.TryFind(text));
    }
}
