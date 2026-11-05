namespace HealthyFac.Shared.UnitTests.ErrorHandling.Data;

public static class ValidationNamesTestData
{
    public static TheoryData<NamedRequest, string> Keys =>
        new()
        {
            { new NamedRequest(), "serialNumber" },
            { new NamedRequest { SerialNumber = "1", Label = string.Empty }, "custom_label" },
            { new NamedRequest { SerialNumber = "1", Child = new NamedChild() }, "child.firstName" },
            {
                new NamedRequest { SerialNumber = "1", Children = [new NamedChild()] },
                "children[0].firstName"
            },
        };
}
