using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Defines error messages for work order operations, grouped by the kind of failure.
/// </summary>
public static class WorkOrderErrors
{
    public static class Validation
    {
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string WorkOrderIdRequired = "WorkOrderId is required.";
        public const string TitleRequired = "The title is required.";
        public const string AssigneeIdRequired = "AssigneeId is required.";
        public const string FileIdsRequired = "The file list is required.";
        public const string FileIdsDuplicate = "A file appears twice in the list.";
        public const string FileIdEmpty = "A file ID must not be empty.";
        public const string AssigneeAndStartTogether = "The assignee and the planned start go together.";
        public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";
        public const string StatusIsBacklog = "A work order cannot change its status to backlog. Unassign it instead.";
        public const string RowVersionRequired = "The row version is required.";
        public const string StatusRequired = "The status is required.";
        public const string PriorityRequired = "The priority is required.";
        public const string CategoryRequired = "The category is required.";

        public static readonly string TitleTooLong =
            $"The title must have at most {WorkOrderValidationConstants.TitleMaxLength} characters.";

        public static readonly string DescriptionTooLong =
            $"The description must have at most {WorkOrderValidationConstants.DescriptionMaxLength} characters.";

        public static readonly string DescriptionTooMuchFormatting =
            "The description formatting is too large. The stored HTML must have at most "
            + $"{Shared.Application.RichText.RichTextConstants.HtmlMaxLength} characters.";

        public static readonly string BatchSizeInvalid =
            $"The batch needs 1 to {WorkOrderValidationConstants.BatchMaxSearches} searches.";

        public const string BatchSearchRequired = "Each search of the batch is required.";

        public static readonly string TooManyFiles =
            $"A work order has at most {WorkOrderValidationConstants.FileMaxCount} files.";

        public static readonly string EstimateOutOfRange =
            $"The estimate must be from {WorkOrderValidationConstants.EstimateMinMinutes} to "
            + $"{WorkOrderValidationConstants.EstimateMaxMinutes} minutes.";

        public static readonly string EstimateOffGrid =
            $"The estimate must be a multiple of {WorkOrderValidationConstants.EstimateStepMinutes} minutes.";

        public static readonly string StartOffGrid =
            $"The planned start must be a multiple of {SlotRules.GridStep.TotalMinutes} minutes.";

        public static readonly string StartOutOfRange =
            "The planned start must be now or later, and at most "
            + $"{Shifts.ShiftValidationConstants.MaxDaysAhead} days ahead.";
    }

    public static class Rejections
    {
        public const string OwnEmployeeNotFound = "The user has no employee in the company.";
        public const string WorkOrderNotFound = "The work order does not exist.";
        public const string MachineNotFound = "The machine does not exist.";
        public const string AssigneeNotFound = "The employee does not exist in the company.";
        public const string AssigneeNotActive = "The employee is not active.";
        public const string OutsideShift = "The slot is not inside one work block of the employee.";
        public const string SlotTaken = "The slot overlaps another work order of the employee.";
        public const string OwnScopeForbidden = "You may not do this with your rights.";
        public const string NotInBacklog = "Only a work order in the backlog can be assigned.";
        public const string NotScheduled = "A work order in the backlog has no assignee and no planned start.";
        public const string SameStatus = "The work order already has this status.";
        public const string WorkOrderChanged = "The work order changed at the same time. Reload it and try again.";
    }
}
