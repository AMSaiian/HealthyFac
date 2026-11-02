using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.UnitTests.TextInput.Data;

public static class TextInputTestData
{
    public const string NullCharacterText = "before\0after";
    public const string TabText = "before\tafter";
    public const string MultilineText = "first line\n\tindented line";
    public const string PlainText = "plain text";

    public static TheoryData<TextInputRequest, string, string> Violations =>
        new()
        {
            { new TextInputRequest { Name = NullCharacterText }, "Name", TextInputErrors.NullCharacter },
            { new TextInputRequest { Notes = NullCharacterText }, "Notes", TextInputErrors.NullCharacter },
            { new TextInputRequest { Token = NullCharacterText }, "Token", TextInputErrors.NullCharacter },
            {
                new TextInputRequest { Address = new TextInputAddress { Street = NullCharacterText } },
                "Address.Street",
                TextInputErrors.NullCharacter
            },
            {
                new TextInputRequest { Tags = [PlainText, NullCharacterText] },
                "Tags[1]",
                TextInputErrors.NullCharacter
            },
            {
                new TextInputRequest { Addresses = [new TextInputAddress { Street = NullCharacterText }] },
                "Addresses[0].Street",
                TextInputErrors.NullCharacter
            },
            { new TextInputRequest { Name = TabText }, "Name", TextInputErrors.ControlCharacter },
            { new TextInputRequest { Tags = [TabText] }, "Tags[0]", TextInputErrors.ControlCharacter },
            { new TextInputRequest { Notes = "bell\a" }, "Notes", TextInputErrors.MultilineControlCharacter },
        };

    public static TheoryData<TextInputRequest> ValidRequests =>
        new()
        {
            new TextInputRequest { Name = PlainText, Tags = [PlainText] },
            new TextInputRequest { Notes = MultilineText },
            new TextInputRequest { Token = TabText },
            new TextInputRequest(),
        };

    /// <summary>
    /// JSON input and the request that the shared options must read from it.
    /// </summary>
    public static TheoryData<string, TextInputRequest> NormalizedJson =>
        new()
        {
            { """{"name":"  Café  "}""", new TextInputRequest { Name = "Café" } },
            { """{"notes":"  first\r\nsecond\rthird  "}""", new TextInputRequest { Notes = "first\nsecond\nthird" } },
            { """{"token":"  raw\r\n  "}""", new TextInputRequest { Token = "  raw\r\n  " } },
            { """{"tags":["  first  ","second "]}""", new TextInputRequest { Tags = ["first", "second"] } },
            {
                """{"address":{"street":"  Main Street  "}}""",
                new TextInputRequest { Address = new TextInputAddress { Street = "Main Street" } }
            },
            { """{"name":null}""", new TextInputRequest { Name = null } },
        };
}
