using System.Text.RegularExpressions;

namespace EmailService.Internal.Extensions;

internal static partial class StringExtensions
{
    public static TimeSpan ToTimeSpan(this string value, string? variableName = null) {
        var match = StringTimespanRegex().Match(value);
        if (!match.Success) {
            throw new ArgumentException("The provided value is not in the correct format for TimeSpan conversion", variableName ?? nameof(value));
        }
        var numericVal = long.Parse(match.Groups[1].Value);
        return match.Groups[2].Value.ToLower() switch {
            "ticks" => TimeSpan.FromTicks(numericVal),
            "milliseconds" => TimeSpan.FromMilliseconds(numericVal),
            "seconds" => TimeSpan.FromSeconds(numericVal),
            "minutes" => TimeSpan.FromMinutes(numericVal),
            "hours" => TimeSpan.FromHours(numericVal),
            _ => throw new ArgumentException("The provided value is not in the correct format for TimeSpan conversion", variableName ?? nameof(value))
        };
    }

    [GeneratedRegex(
        @"^(\d+)\s+(ticks?|milliseconds?|seconds?|minutes?|hours?)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex StringTimespanRegex();
}
