using HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;
using HealthyFac.Core.Application.Employees.VerifyEmployeeInvite;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;

public static class InviteTokenLoggingTestData
{
    public const string Token = "0190a1b2c3d4.k1.c2lnbmF0dXJl";
    public const string TokenPropertyName = "Token";

    public static TheoryData<object> RequestsWithToken => new()
    {
        new VerifyEmployeeInviteRequest { Token = Token },
        new AcceptEmployeeInviteRequest { Token = Token, FirstName = "Eva", LastName = "Stone" },
    };
}
