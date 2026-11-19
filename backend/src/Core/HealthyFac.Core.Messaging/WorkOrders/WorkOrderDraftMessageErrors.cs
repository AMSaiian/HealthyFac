namespace HealthyFac.Core.Messaging.WorkOrders;

public static class WorkOrderDraftMessageErrors
{
    public static class Validation
    {
        public const string IdRequired = "Every ID of the message is required.";
        public const string TimeRequired = "The time of the message is required.";
    }
}
