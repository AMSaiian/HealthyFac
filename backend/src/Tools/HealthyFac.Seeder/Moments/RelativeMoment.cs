using System.Globalization;
using System.Text.RegularExpressions;

namespace HealthyFac.Seeder.Moments;

/// <summary>
/// A moment that the seed files state relative to the job start: <c>wd±N HH:mm</c>, <c>d±N HH:mm</c>, <c>d±N</c>,
/// <c>h±N</c>, or <c>h±N:MM</c>. <see cref="SeedCalendar"/> turns it into a UTC instant or a date.
/// </summary>
public sealed partial record RelativeMoment
{
    private const string TimeFormat = "HH:mm";
    private const string WorkdayPrefix = "wd";
    private const string DayPrefix = "d";
    private const string HourPrefix = "h";
    private const int MaxMinutes = 59;

    /// <summary>The minutes of one hour, for the hour form.</summary>
    public const int MinutesPerHour = 60;

    public required MomentForm Form { get; init; }

    /// <summary>The signed count of workdays, days, or hours from the anchor.</summary>
    public required int Offset { get; init; }

    /// <summary>
    /// The minutes of the hour form (0-59), added with the sign of the moment. It is 0 for every other form.
    /// </summary>
    public int Minutes { get; init; }

    /// <summary>
    /// <see langword="true"/> for an hour form written with <c>-</c>. It keeps the sign when <see cref="Offset"/> is 0
    /// (<c>h-0:30</c>).
    /// </summary>
    public bool IsBefore { get; init; }

    /// <summary>
    /// The signed minutes of the hour form from the anchor (<c>h-22:05</c> is -1325). It is 0 for every other form.
    /// </summary>
    public int TotalMinutes =>
        Form == MomentForm.Hour ? Sign * (Math.Abs(Offset) * MinutesPerHour + Minutes) : 0;

    /// <summary>
    /// -1 for a moment before the anchor, also when <see cref="Offset"/> is 0 (<c>h-0:30</c>); else 1.
    /// </summary>
    private int Sign => Offset < 0 || IsBefore ? -1 : 1;

    /// <summary>Makes the hour form of a signed number of minutes. Zero and a negative number read as before.</summary>
    /// <param name="totalMinutes">The signed minutes from the anchor.</param>
    /// <returns>The moment, with the sign of a zero as <c>h-0</c>.</returns>
    public static RelativeMoment FromTotalMinutes(int totalMinutes) =>
        new()
        {
            Form = MomentForm.Hour,
            Offset = (totalMinutes <= 0 ? -1 : 1) * (Math.Abs(totalMinutes) / MinutesPerHour),
            Minutes = Math.Abs(totalMinutes) % MinutesPerHour,
            IsBefore = totalMinutes <= 0,
        };

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

        var isTimeGiven = match.Groups["time"].Success;
        var prefix = match.Groups["prefix"].Value;

        var isNumberRead = int.TryParse(
            match.Groups["number"].Value, CultureInfo.InvariantCulture, out var number);

        var isTimeRead = TimeOnly.TryParseExact(
            match.Groups["time"].Value,
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time);

        var isWorkdayWithoutTime = prefix == WorkdayPrefix && !isTimeGiven;
        var isHourWithTime = prefix == HourPrefix && isTimeGiven;
        var isMinutesGiven = match.Groups["minutes"].Success;

        var isMinutesRead = int.TryParse(
            match.Groups["minutes"].Value, CultureInfo.InvariantCulture, out var minutes);

        var isMinutesInvalid = isMinutesGiven && (prefix != HourPrefix || !isMinutesRead || minutes > MaxMinutes);

        if (!isNumberRead
            || (isTimeGiven && !isTimeRead)
            || isWorkdayWithoutTime
            || isHourWithTime
            || isMinutesInvalid)
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
            Offset = match.Groups["sign"].Value == "-" ? -number : number,
            Minutes = isMinutesGiven ? minutes : 0,
            IsBefore = prefix == HourPrefix && match.Groups["sign"].Value == "-",
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

        var sign = Offset < 0 || IsBefore ? '-' : '+';

        var time = Time is { } timeOfDay
            ? $" {timeOfDay.ToString(TimeFormat, CultureInfo.InvariantCulture)}"
            : string.Empty;

        var minutes = Minutes > 0 ? $":{Minutes.ToString(format: "00", CultureInfo.InvariantCulture)}" : string.Empty;

        return $"{prefix}{sign}{Math.Abs(Offset).ToString(CultureInfo.InvariantCulture)}{minutes}{time}";
    }

    [GeneratedRegex(
        @"^(?<prefix>wd|d|h)(?<sign>[+-])(?<number>[0-9]+)(?::(?<minutes>[0-9]{2}))?"
        + @"(?: (?<time>[0-9]{2}:[0-9]{2}))?\z")]
    private static partial Regex MomentPattern();
}
