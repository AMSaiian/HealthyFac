using System.Text.Json;

namespace HealthyFac.Shared.UnitTests.Time.Data;

public static class LocalTimesTestData
{
    public const string ZoneId = "Europe/Kyiv";
    public const string LocalFormat = "yyyy-MM-ddTHH:mm";
    public const string UtcFormat = "yyyy-MM-ddTHH:mmZ";

    private const string DstFileName = "dst-europe-kyiv-2027.json";

    public static TheoryData<string, string> UtcCases
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var dstCase in LoadCases())
            {
                data.Add(dstCase.Local, dstCase.Utc);
            }

            return data;
        }
    }

    public static TheoryData<string, bool> SkippedCases
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (var dstCase in LoadCases())
            {
                data.Add(dstCase.Local, dstCase.IsSkipped);
            }

            return data;
        }
    }

    private static List<DstCase> LoadCases() =>
        JsonSerializer.Deserialize<List<DstCase>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path2: "Time", path3: "Data", DstFileName)),
            JsonSerializerOptions.Web)!;

    private sealed class DstCase
    {
        public required string Local { get; init; }

        public required string Utc { get; init; }

        public required bool IsSkipped { get; init; }
    }
}
