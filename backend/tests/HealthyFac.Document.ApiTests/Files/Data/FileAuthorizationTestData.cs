namespace HealthyFac.Document.ApiTests.Files.Data;

public static class FileAuthorizationTestData
{
    public static TheoryData<string, string> UploadEndpoints => new()
    {
        { UploadFileApiTestData.CompanyFilesPath, UploadFileApiTestData.FilesWritePermission },
        { UploadFileApiTestData.EmbeddedPath, UploadFileApiTestData.CompanyWritePermission },
        { MachinePlaceholderPath, UploadFileApiTestData.CompanyWritePermission },
    };

    public static TheoryData<string> UploadPaths => new()
    {
        { UploadFileApiTestData.CompanyFilesPath },
        { UploadFileApiTestData.EmbeddedPath },
        { MachinePlaceholderPath },
    };

    /// <summary>
    /// The path of the machine upload. The test replaces the segment with a real encrypted machine ID.
    /// </summary>
    public const string MachinePlaceholderPath = "v1/machines/{machineId}/files";
}
