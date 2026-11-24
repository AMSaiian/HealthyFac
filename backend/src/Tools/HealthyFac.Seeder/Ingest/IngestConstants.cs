namespace HealthyFac.Seeder.Ingest;

public static class IngestConstants
{
    public const string SetFilePattern = "ingest/set-{0}.json";
    public const string EmbeddingsFileName = "ingest/embeddings.json";
    public const string FileSuffix = ".file";
    public const string AttemptSuffix = ".attempt";
    public const string UploadSuffix = ".upload";
    public const string IngestSuffix = ".ingest";

    public static readonly IReadOnlyList<int> SetNumbers = [1, 2];

    public static readonly TimeSpan LeaseLength = TimeSpan.FromMinutes(minutes: 5);

    public static readonly IReadOnlyDictionary<string, string> ContentTypes = new Dictionary<string, string>
    {
        [key: ".pdf"] = "application/pdf",
        [key: ".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [key: ".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };
}
