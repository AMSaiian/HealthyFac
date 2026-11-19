namespace HealthyFac.Insights.Messaging.WorkOrders;

/// <summary>
/// The limits of the <see cref="WorkOrderDraftProposed"/> message. Core owns the work order rules, and a Core unit test
/// checks that these values equal the Core constants, so the two definitions cannot drift apart.
/// </summary>
public static class WorkOrderDraftMessageLimits
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 5000;
    public const int EstimateMinMinutes = 5;
    public const int EstimateMaxMinutes = 720;
    public const int EstimateStepMinutes = 5;

    public static class Priorities
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Critical = "critical";

        public static readonly string[] All = [Low, Medium, High, Critical];
    }

    public static class Categories
    {
        public const string Inspection = "inspection";
        public const string Preventive = "preventive";
        public const string Repair = "repair";
        public const string Other = "other";

        public static readonly string[] All = [Inspection, Preventive, Repair, Other];
    }
}
