namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatReadToolTestData
{
    /// <summary>The case name, the tool name, and the arguments JSON with the placeholder <c>%ID%</c>.</summary>
    public static TheoryData<string, string, string> NotFoundTools => new()
    {
        { "SearchMachines", "search_machines", """{"name":"Pre"}""" },
        { "GetWorkOrder", "get_work_order", """{"workOrderId":"%ID%"}""" },
    };
}
