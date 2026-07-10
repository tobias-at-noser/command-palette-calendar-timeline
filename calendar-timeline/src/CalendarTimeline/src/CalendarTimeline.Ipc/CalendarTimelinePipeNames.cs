using System.Security.Cryptography;
using System.Text;

namespace CalendarTimeline.Ipc;

public static class CalendarTimelinePipeNames
{
    public static string Default => FormatPipeNameForUser(Environment.UserName);

    public static string FormatPipeNameForUser(string userName)
    {
        return $"calendar-timeline-{Sanitize(userName)}-{CreateUserHashSuffix(userName)}";
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-')
            {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? "user" : builder.ToString();
    }

    private static string CreateUserHashSuffix(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash[..4]).ToLowerInvariant();
    }
}
