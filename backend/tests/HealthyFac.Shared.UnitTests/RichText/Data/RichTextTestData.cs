namespace HealthyFac.Shared.UnitTests.RichText.Data;

public static class RichTextTestData
{
    public const string FileId = "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public static TheoryData<string> FileImageSamples => new()
    {
        $"<p><img data-file-id=\"{FileId}\" alt=\"pump\"></p>",
        $"<p><img data-file-id=\"{FileId}\"></p>",
        $"<p><img data-file-id=\"{FileId}\" alt=\"{new string(c: 'a', count: 255)}\"></p>",
    };

    public static TheoryData<string> RejectedFileImages => new()
    {
        "<p><img src=\"https://x/y.png\"></p>",
        "<p><img src=\"data:image/png;base64,AA==\"></p>",
        "<p><img data-file-id=\"abc\"></p>",
        $"<p><img data-file-id=\"{FileId}\" style=\"width:1px\"></p>",
        $"<p><img data-file-id=\"{FileId}\" width=\"1\"></p>",
        $"<p><img data-file-id=\"{FileId}\" src=\"https://x/y.png\"></p>",
        "<p><img alt=\"x\"></p>",
        $"<p><img data-file-id=\"{FileId}&#10;\"></p>",
        "<p alt=\"x\">a</p>",
        $"<p><span data-file-id=\"{FileId}\">a</span></p>",
        $"<p><img data-file-id=\"{FileId}\" alt=\"{new string(c: 'a', count: 256)}\"></p>",
    };

    public static TheoryData<string> ToolbarSamples => new()
    {
        "<p><strong>a</strong></p>",
        "<p><em>a</em></p>",
        "<p><u>a</u></p>",
        "<p><s>a</s></p>",
        "<ol><li data-list=\"bullet\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li></ol>",
        "<ol><li data-list=\"ordered\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li></ol>",
        "<ol><li data-list=\"checked\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li>"
        + "<li data-list=\"unchecked\"><span class=\"ql-ui\" contenteditable=\"false\"></span>b</li></ol>",
        "<p><a href=\"https://example.com\" rel=\"noopener noreferrer\" target=\"_blank\">a</a></p>",
        "<p><a href=\"mailto:a@example.com\" rel=\"noopener noreferrer\" target=\"_blank\">a</a></p>",
        "<p><span class=\"ql-size-small\">a</span></p>",
        "<p><span class=\"ql-size-large\">a</span></p>",
        "<p><span class=\"ql-size-huge\">a</span></p>",
        "<p>a<br></p>",
        "<p><br></p>",
        "<ul><li>a</li></ul>",
        "<ol><li>a</li></ol>",
        "<p><strong>a&nbsp;</strong><em>b&nbsp;</em><u>c</u><s>d</s></p>",
        "<ol><li><a href=\"https://example.com\" rel=\"noopener noreferrer\" target=\"_blank\">first</a></li></ol>"
        + "<ul><li>b</li></ul>",
    };

    public static TheoryData<string> RejectedDataLists => new()
    {
        "<ol><li data-list=\"x\">a</li></ol>",
        "<ol><li data-list=\"\">a</li></ol>",
        "<ol><li data-list=\"Checked\">a</li></ol>",
        "<ol><li data-list=\"checked unchecked\">a</li></ol>",
        "<ol><li data-list=\"bullet\">a</li><li data-list=\"todo\">b</li></ol>",
    };

    public static TheoryData<string> HostileHtml => new()
    {
        "<script>alert(1)</script>",
        "<p>a</p><script>alert(1)</script>",
        "<img src=x onerror=alert(1)>",
        "<p><a href=\"javascript:alert(1)\">a</a></p>",
        "<p style=\"color:red\">a</p>",
        "<p><span class=\"other\">a</span></p>",
        "<p><span class=\"ql-size-gigantic\">a</span></p>",
        "<iframe src=\"https://example.com\"></iframe>",
        "<p onclick=\"alert(1)\">a</p>",
        "<p><!-- comment -->a</p>",
        "<h1>a</h1>",
    };

    public static TheoryData<string, int> VisibleLengthCases => new()
    {
        { "<p>a</p>", 1 },
        { "<p><br></p>", 0 },
        { "<p>&amp;</p>", 1 },
        { "<ol><li data-list=\"bullet\"><span class=\"ql-ui\" contenteditable=\"false\"></span>ab</li></ol>", 2 },
        {
            "<ol><li data-list=\"checked\"><span class=\"ql-ui\" contenteditable=\"false\"></span>ab</li>"
            + "<li data-list=\"unchecked\"><span class=\"ql-ui\" contenteditable=\"false\"></span>c</li></ol>",
            3
        },
        { "<p>e" + (char)0x0301 + "</p>", 1 },
        { "<p>e&#x301;</p>", 1 },
        { "<p>" + (char)0x0958 + "</p>", 2 },
        { "<p><strong>ab</strong> <em>c</em></p>", 4 },
        { $"<p>ab<img data-file-id=\"{FileId}\"></p>", 2 },
    };
}
