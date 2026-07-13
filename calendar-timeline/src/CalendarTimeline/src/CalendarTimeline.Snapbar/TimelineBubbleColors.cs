namespace CalendarTimeline.Snapbar;

public static class TimelineBubbleColors
{
    private const double MinimumTextContrast = 4.5;
    private static readonly double[] GradientAmounts = [0.05, 0.04, 0.03, 0.02, 0.01, 0];
    private static readonly string[] Palette =
    [
        "#4F46E5",
        "#0F766E",
        "#B45309",
        "#BE123C",
        "#0369A1",
        "#6D28D9",
    ];

    public static TimelineBubbleColorSet Resolve(
        IReadOnlyList<string?> categoryColors,
        string? calendarColor,
        string calendarIdentity)
    {
        var fallback = Palette[(int)(GetStableHash(calendarIdentity) % (uint)Palette.Length)];
        var category = categoryColors.Select(Normalize).FirstOrDefault(color => color is not null);
        var calendar = Normalize(calendarColor);
        var fill = category ?? calendar ?? fallback;
        var border = category is null && calendar is not null
            ? GetContrastingColor(fill)
            : calendar ?? fallback;
        var (lightFill, darkFill, foreground) = GetAccessibleGradient(fill);
        return new TimelineBubbleColorSet(fill, border, foreground, lightFill, darkFill);
    }

    private static string? Normalize(string? color)
    {
        if (color is null || color.Length != 7 || color[0] != '#')
        {
            return null;
        }

        for (var index = 1; index < color.Length; index++)
        {
            if (!Uri.IsHexDigit(color[index]))
            {
                return null;
            }
        }

        return color.ToUpperInvariant();
    }

    private static string GetContrastingColor(string color)
    {
        return GetRelativeLuminance(color) > 0.179 ? "#000000" : "#FFFFFF";
    }

    private static (string LightFill, string DarkFill, string Foreground) GetAccessibleGradient(string fill)
    {
        foreach (var amount in GradientAmounts)
        {
            var lightFill = Adjust(fill, amount, lighten: true);
            var darkFill = Adjust(fill, amount, lighten: false);
            var foreground = GetAccessibleForeground(lightFill, darkFill);
            if (foreground is not null)
            {
                return (lightFill, darkFill, foreground);
            }
        }

        throw new InvalidOperationException("An accessible foreground could not be selected.");
    }

    private static string? GetAccessibleForeground(string lightFill, string darkFill)
    {
        var blackContrast = Math.Min(
            GetContrastRatio(lightFill, "#000000"),
            GetContrastRatio(darkFill, "#000000"));
        var whiteContrast = Math.Min(
            GetContrastRatio(lightFill, "#FFFFFF"),
            GetContrastRatio(darkFill, "#FFFFFF"));

        if (blackContrast < MinimumTextContrast && whiteContrast < MinimumTextContrast)
        {
            return null;
        }

        return blackContrast >= whiteContrast ? "#000000" : "#FFFFFF";
    }

    private static string Adjust(string color, double amount, bool lighten)
    {
        var red = Convert.ToInt32(color.Substring(1, 2), 16);
        var green = Convert.ToInt32(color.Substring(3, 2), 16);
        var blue = Convert.ToInt32(color.Substring(5, 2), 16);
        return $"#{Adjust(red, amount, lighten):X2}{Adjust(green, amount, lighten):X2}{Adjust(blue, amount, lighten):X2}";
    }

    private static int Adjust(int component, double amount, bool lighten)
    {
        return (int)Math.Round(lighten
            ? component + ((255 - component) * amount)
            : component * (1 - amount));
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
        var luminance = (0.2126 * ToLinear(red))
            + (0.7152 * ToLinear(green))
            + (0.0722 * ToLinear(blue));
        return luminance;
    }

    private static double ToLinear(double component)
    {
        return component <= 0.04045
            ? component / 12.92
            : Math.Pow((component + 0.055) / 1.055, 2.4);
    }

    private static uint GetStableHash(string value)
    {
        var hash = 2166136261u;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16777619;
        }

        return hash;
    }
}

public readonly record struct TimelineBubbleColorSet(
    string Fill,
    string Border,
    string Foreground,
    string LightFill,
    string DarkFill);
