namespace HealthyFac.Core.Infrastructure.Shifts.Persistence;

/// <summary>
/// Defines the database index names of the shift tables that code reads, so a name never differs between the mapping
/// and a check.
/// </summary>
public static class ShiftIndexNames
{
    public const string TemplateVersionEffectiveFrom = "ix_shift_template_versions_employee_id_effective_from";
    public const string OverrideDate = "ix_shift_overrides_employee_id_date";
}
