using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineBubbleColorsTests
{
    [Fact]
    public void Resolve_PrefersTheNormalizedCategoryForFillAndCalendarForBorder()
    {
        var colors = TimelineBubbleColors.Resolve(["#d83b01"], "#3b82b6", "work");

        Assert.Equal("#D83B01", colors.Fill);
        Assert.Equal("#3B82B6", colors.Border);
    }

    [Fact]
    public void Resolve_UsesTheFirstValidCategoryColorInOutlookOrder()
    {
        var colors = TimelineBubbleColors.Resolve(["not-a-color", "#d83b01"], "#3b82b6", "work");

        Assert.Equal("#D83B01", colors.Fill);
        Assert.Equal("#3B82B6", colors.Border);
    }

    [Fact]
    public void Resolve_UsesCalendarForFillAndContrastingBorderWhenCategoryIsMissing()
    {
        var colors = TimelineBubbleColors.Resolve([], "#3B82B6", "work");

        Assert.Equal("#3B82B6", colors.Fill);
        Assert.NotEqual(colors.Fill, colors.Border);
        Assert.True(GetContrastRatio(colors.Fill, colors.Border) >= 1.5);
    }

    [Fact]
    public void Resolve_UsesAStableIdentityFallbackForInvalidOrMissingColors()
    {
        var first = TimelineBubbleColors.Resolve(["#D83B0"], "blue", "work");
        var second = TimelineBubbleColors.Resolve([], null, "work");

        Assert.Equal(first.Fill, second.Fill);
        Assert.Equal(first.Border, second.Border);
        Assert.Matches("^#[0-9A-F]{6}$", first.Fill);
        Assert.Matches("^#[0-9A-F]{6}$", first.Border);
    }

    [Theory]
    [InlineData("#FFFFFF", "#000000")]
    [InlineData("#000000", "#FFFFFF")]
    [InlineData("#D83B01", "#FFFFFF")]
    public void Resolve_ChoosesAnAccessibleForeground(string fill, string expectedForeground)
    {
        var colors = TimelineBubbleColors.Resolve([fill], null, "work");

        Assert.Equal(expectedForeground, colors.Foreground);
        Assert.True(GetContrastRatio(colors.Fill, colors.Foreground) >= 4.5);
    }

    [Theory]
    [InlineData("#D83B01")]
    [InlineData("#3B82B6")]
    [InlineData("#B45309")]
    [InlineData("#4F46E5")]
    [InlineData("#0F766E")]
    [InlineData("#BE123C")]
    [InlineData("#0369A1")]
    [InlineData("#6D28D9")]
    public void Resolve_UsesAnAccessibleForegroundAcrossBothGradientStops(string fill)
    {
        var colors = TimelineBubbleColors.Resolve([fill], null, "work");

        Assert.True(GetContrastRatio(colors.LightFill, colors.Foreground) >= 4.5);
        Assert.True(GetContrastRatio(colors.DarkFill, colors.Foreground) >= 4.5);
    }

    private static double GetContrastRatio(string first, string second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05)
            / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static double GetRelativeLuminance(string color)
    {
        var red = Convert.ToInt32(color.Substring(1, 2), 16) / 255d;
        var green = Convert.ToInt32(color.Substring(3, 2), 16) / 255d;
        var blue = Convert.ToInt32(color.Substring(5, 2), 16) / 255d;
        return (0.2126 * ToLinear(red)) + (0.7152 * ToLinear(green)) + (0.0722 * ToLinear(blue));
    }

    private static double ToLinear(double component)
    {
        return component <= 0.04045
            ? component / 12.92
            : Math.Pow((component + 0.055) / 1.055, 2.4);
    }
}
