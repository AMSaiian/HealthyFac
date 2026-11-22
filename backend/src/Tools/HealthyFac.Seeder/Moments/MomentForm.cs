namespace HealthyFac.Seeder.Moments;

public enum MomentForm
{
    /// <summary><c>wd±N HH:mm</c>: workdays from the local seed day, with a local time.</summary>
    Workday,

    /// <summary><c>d±N HH:mm</c>: calendar days from the local seed day, with a local time.</summary>
    Day,

    /// <summary><c>d±N</c>: calendar days from the local seed day, as a date.</summary>
    Date,

    /// <summary><c>h±N</c>: hours from the anchor, exact.</summary>
    Hour,
}
