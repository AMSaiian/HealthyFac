namespace HealthyFac.Document.ApiTests.Files.Data;

public static class TieFileTestData
{
    public const string WritePermission = "company-files:write";
    public const string ReadPermission = "files:read";
    public const string SearchBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";

    public static string MachineOfFilePath(string encryptedFileId) =>
        $"{UploadFileApiTestData.CompanyFilesPath}/{encryptedFileId}/machine";

    public static string TieBody(string encryptedMachineId, uint rowVersion) =>
        $"{{\"machineId\":\"{encryptedMachineId}\",\"rowVersion\":{rowVersion}}}";

    public static string UntieBody(uint rowVersion) => $"{{\"rowVersion\":{rowVersion}}}";
}
