namespace HealthyFac.Insights.Messaging.WorkOrders;

public static class WorkOrderDraftMessageErrors
{
    public static class Validation
    {
        public const string IdRequired = "Every ID of the message is required.";
        public const string SourceRequired = "The source is required.";
        public const string TitleRequired = "The title is required.";
        public const string FieldsOfAnotherSource = "A field of the other source must be empty.";
        public const string PriorityInvalid = "The priority is not one of the allowed values.";
        public const string CategoryInvalid = "The category is not one of the allowed values.";

        public static readonly string TitleTooLong =
            $"The title must have at most {WorkOrderDraftMessageLimits.TitleMaxLength} characters.";

        public static readonly string DescriptionTooLong =
            $"The description must have at most {WorkOrderDraftMessageLimits.DescriptionMaxLength} characters.";

        public static readonly string EstimateInvalid =
            $"The estimate must be between {WorkOrderDraftMessageLimits.EstimateMinMinutes} and "
            + $"{WorkOrderDraftMessageLimits.EstimateMaxMinutes} minutes, in steps of "
            + $"{WorkOrderDraftMessageLimits.EstimateStepMinutes} minutes.";
    }
}
