using System.Text;
using HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Shared.UnitTests.HttpFailures.Data;

public static class HttpFailureTestData
{
    public const string RequestUrl = "https://keycloak.test/admin/realms/healthyfac/users";
    public const string RequestUrlWithQuery = $"{RequestUrl}?search=person@example.com";
    public const string RequestPath = "/admin/realms/healthyfac/users";
    public const string QueryValue = "person@example.com";
    public const string ProblemDetail = "The invite is expired.";
    public const string HtmlBody = "<html><body>Gateway timeout secret page</body></html>";
    public const string HtmlText = "secret page";
    public const string KeycloakMessage = "Invalid redirect uri";
    public const string KeycloakParamEmail = "person@example.com";
    public const string SecondReason = "second reason";
    public const string NestedReasonBody = """{"error":{"detail":"nested reason","error_description":"nested"}}""";
    public const int OverLimitReasonLength = 2500;
    public const char KeptCharacter = 'a';
    public const char CutCharacter = 'b';

    public static readonly string KeycloakValidationBody =
        $$"""{"field":"email","errorMessage":"{{KeycloakMessage}}","params":["{{KeycloakParamEmail}}"]}""";

    public static readonly string ProblemDetailAndOAuthBody =
        $$"""{"error":"invalid_client","detail":"{{ProblemDetail}}"}""";

    public static readonly string TopLevelAfterNestedBody =
        $$"""{"error":{"detail":"nested reason"},"detail":"{{ProblemDetail}}"}""";

    public static TheoryData<string, string> BodiesWithReason =>
        new()
        {
            { $$"""{"detail":"{{ProblemDetail}}","title":"Conflict"}""", ProblemDetail },
            { """{"title":"Conflict","status":409}""", "Conflict" },
            { $$"""{"errorMessage":"{{KeycloakMessage}}"}""", KeycloakMessage },
            {
                """{"error":"invalid_client","error_description":"Invalid client credentials"}""",
                "invalid_client: Invalid client credentials"
            },
            { """{"error_description":"Invalid client credentials"}""", "Invalid client credentials" },
            { """{"error":"invalid_grant"}""", "invalid_grant" },
        };

    public static TheoryData<string> BodiesWithoutReason =>
        new()
        {
            """{"message":"unknown"}""",
            """{"detail":"","title":"  "}""",
            """{"detail":42,"error":null}""",
            """["detail"]""",
            """ "text" """,
        };

    public static IReadOnlyDictionary<string, JsonTopLevelField> Parse(string json) =>
        JsonTopLevelFields.Read(Encoding.UTF8.GetBytes(json), true);

    public static string LongReason() =>
        new string(KeptCharacter, HttpFailureConstants.MaxReasonLength)
        + new string(CutCharacter, OverLimitReasonLength - HttpFailureConstants.MaxReasonLength);

    public static string LongBody() =>
        $$"""{"detail":"{{new string(KeptCharacter, HttpFailureConstants.MaxBodyBytes)}}"}""";

    public static string BodyWithReasonBeforeCut() =>
        $$"""{"title":"Bad Request","detail":"{{ProblemDetail}}","errors":{{LongErrors()}}}""";

    public static string BodyWithReasonAfterCut() =>
        $$"""{"errors":{{LongErrors()}},"detail":"{{ProblemDetail}}"}""";

    private static string LongErrors() =>
        $$"""{"name":["{{new string(KeptCharacter, HttpFailureConstants.MaxBodyBytes)}}"]}""";
}
