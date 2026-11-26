using Grpc.Core;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public static class GrpcChatReadsTestData
{
    /// <summary>The case name and a status that the service answers when the user may not read an entity.</summary>
    public static TheoryData<string, StatusCode> DeniedStatuses => new()
    {
        { "NotFound", StatusCode.NotFound },
        { "PermissionDenied", StatusCode.PermissionDenied },
    };
}
