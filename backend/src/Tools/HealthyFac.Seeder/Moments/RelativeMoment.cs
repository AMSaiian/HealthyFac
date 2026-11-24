using System.Globalization;
using System.Text.RegularExpressions;

namespace HealthyFac.Seeder.Moments;

/// <summary>
/// A moment that the seed files state relative to the job start: <c>wd±N HH:mm</c>, <c>d±N HH:mm</c>, <c>d±N</c>,
/// or <c>h±N</c>. <see cref="SeedCalendar"/> turns it into a UTC instant or a date.
/// </summary>
public sealed partial record RelativeMoment
{
    private const string TimeFormat = "HH:mm";
    private const string WorkdayPrefix = "wd";
    private const string DayPrefix = "d";
    private const string HourPrefix = "h";

    public required MomentForm Form { get; init; }

    /// <summary>The signed count of workdays, days, or hours from the anchor.</summary>
    public required int Offset { get; init; }

    /// <summary>The local time of day. It is <see langword="null"/> for the forms that have no time.</summary>
    public TimeOnly? Time { get; init; }

    /// <summary>Reads a moment from its text.</summary>
    /// <param name="text">The text of the moment.</param>
    /// <returns>The parsed moment.</returns>
    /// <exception cref="MomentException">The text does not follow one of the forms.</exception>
    public static RelativeMoment Parse(string text)
    {
        var match = MomentPattern().Match(text);
        if (!match.Success)
        {
            throw new MomentException(MomentErrors.InvalidForm(text));
        }

        var isTimeGiven = match.Groups[groupname: "time"].Success;
        var prefix = match.Groups[groupname: "prefix"].Value;
        var isNumberRead = int.TryParse(
            match.Groups[groupname: "number"].Value, CultureInfo.InvariantCulture, out var number);
        var isTimeRead = TimeOnly.TryParseExact(
            match.Groups[groupname: "time"].Value,
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time);
        var isWorkdayWithoutTime = prefix == WorkdayPrefix && !isTimeGiven;
        var isHourWithTime = prefix == HourPrefix && isTimeGiven;
        if (!isNumberRead || (isTimeGiven && !isTimeRead) || isWorkdayWithoutTime || isHourWithTime)
        {
            throw new MomentException(MomentErrors.InvalidForm(text));
        }

        return new RelativeMoment
        {
            Form = prefix switch
            {
                WorkdayPrefix => MomentForm.Workday,
                HourPrefix => MomentForm.Hour,
                _ => isTimeGiven ? MomentForm.Day : MomentForm.Date,
            },
            Offset = match.Groups[groupname: "sign"].Value == "-" ? -number : number,
            Time = isTimeGiven ? time : null,
        };
    }

    public override string ToString()
    {
        var prefix = Form switch
        {
            MomentForm.Workday => WorkdayPrefix,
            MomentForm.Hour => HourPrefix,
            _ => DayPrefix,
        };
        var sign = Offset < 0 ? '-' : '+';
        var time = Time is { } timeOfDay
            ? $" {timeOfDay.ToString(TimeFormat, CultureInfo.InvariantCulture)}"
            : string.Empty;

        return $"{prefix}{sign}{Math.Abs(Offset).ToString(CultureInfo.InvariantCulture)}{time}";
    }

    [GeneratedRegex(pattern: @"^(?<prefix>wd|d|h)(?<sign>[+-])(?<number>[0-9]+)(?: (?<time>[0-9]{2}:[0-9]{2}))?\z")]
    private static partial Regex MomentPattern();
}
