namespace HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

/// <summary>
/// Defines the naming parts and SQL templates that the check constraint builder uses.
/// </summary>
public static class CheckConstraintConstants
{
    /// <summary>
    /// PostgreSQL cuts longer identifiers silently, which makes migrations drift.
    /// </summary>
    public const int MaxIdentifierLength = 63;

    public const string ValueQuote = "'";
    public const string ValueSeparator = ", ";

    public static class Names
    {
        public const string Prefix = "ck";
        public const string Separator = "_";
        public const string NotBlank = "not_blank";
        public const string Length = "length";
        public const string Range = "range";
        public const string NotEmpty = "not_empty";
        public const string Allowed = "allowed";
    }

    public static class Templates
    {
        public const string NotBlank = @"""{0}"" ~ '\S'";
        public const string Length = @"char_length(""{0}"") BETWEEN {1} AND {2}";
        public const string Range = @"""{0}"" BETWEEN {1} AND {2}";
        public const string NotEmptyGuid = @"""{0}"" <> '{1}'";
        public const string Allowed = @"""{0}"" IN ({1})";
    }
}
