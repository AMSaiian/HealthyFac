namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

public static class StatusDefinitionTestData
{
    public const string Code = "E-101";
    public const string CodeOtherCase = "e-101";
    public const string OtherCode = "E-202";
    public const string ThirdCode = "E-303";
    public const string UpdatedCode = "E-909";

    public const string Description = "<p>Check the <strong>spindle</strong> belt.</p>";
    public const string UpdatedDescription = "<p>Replace the belt.</p>";

    public const int DescriptionLimit = 5000;

    /// <summary>5000 visible characters in 500 bold runs: about 13,500 HTML characters.</summary>
    public static string FormattedDescriptionAtVisibleLimit =>
        "<p>"
        + string.Concat(Enumerable.Repeat(element: "<strong>aaaaaaaaaa</strong>", DescriptionLimit / 10))
        + "</p>";

    public const string FirstMachineName = "Lathe 01";
    public const string FirstMachineSerialNumber = "EC-1";
    public const string SecondMachineName = "Press 02";
    public const string SecondMachineSerialNumber = "EC-2";
}
