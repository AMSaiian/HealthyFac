namespace HealthyFac.Shared.UnitTests.RichText.Data;

public static class RichTextTestData
{
    public static TheoryData<string> ToolbarSamples => new()
    {
        "<p><strong>a</strong></p>",
        "<p><em>a</em></p>",
        "<p><u>a</u></p>",
        "<p><s>a</s></p>",
        "<ol><li data-list=\"bullet\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li></ol>",
        "<ol><li data-list=\"ordered\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li></ol>",
        "<p><a href=\"https://example.com\" rel=\"noopener noreferrer\" target=\"_blank\">a</a></p>",
        "<p><a href=\"mailto:a@example.com\" rel=\"noopener noreferrer\" target=\"_blank\">a</a></p>",
        "<p>a<br></p>",
        "<p><br></p>",
        "<ul><li>a</li></ul>",
        "<ol><li>a</li></ol>",
        "<p><strong>a&nbsp;</strong><em>b&nbsp;</em><u>c</u><s>d</s></p>",
        "<ol><li><a href=\"https://example.com\" rel=\"noopener noreferrer\" target=\"_blank\">first</a></li></ol><ul><li>b</li></ul>",
    };

    public static TheoryData<string> HostileHtml => new()
    {
        "<script>alert(1)</script>",
        "<p>a</p><script>alert(1)</script>",
        "<img src=x onerror=alert(1)>",
        "<p><a href=\"javascript:alert(1)\">a</a></p>",
        "<p style=\"color:red\">a</p>",
        "<p><span class=\"other\">a</span></p>",
        "<iframe src=\"https://example.com\"></iframe>",
        "<p onclick=\"alert(1)\">a</p>",
        "<p><!-- comment -->a</p>",
        "<h1>a</h1>",
    };
}
