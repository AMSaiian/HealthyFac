using HealthyFac.Document.Application.Files;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class GetFileLinksTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a31");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a32");

    public static readonly DateTimeOffset FixedNow = new(
        year: 2026, month: 5, day: 1, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public const string MalformedId = "not-an-encrypted-id";
    public const int LifetimeSeconds = 300;
    public const string SasTimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    public const string PublicBaseUrl = "https://api.healthyfac.test/api/document/blobs";

    public static readonly DateTime UploadedAt = new(
        year: 2026, month: 4, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static string[] IdsAboveTheLimit =>
        [.. Enumerable.Range(start: 0, FileValidationConstants.LinkIdsMaxCount + 1).Select(_ => "id")];

    public static string[] IdsAtTheLimit =>
        [.. Enumerable.Range(start: 0, FileValidationConstants.LinkIdsMaxCount).Select(_ => "id")];
}
