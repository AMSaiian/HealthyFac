namespace HealthyFac.Insights.UnitTests.Conversion.Data;

public static class DoclingTestData
{
    public const string ApiKey = "test-docling-key";
    public const string BaseUrl = "http://docling.test";
    public const int PollSeconds = 2;
    public const int MaxPages = 500;
    public const string FileContent = "file-bytes-for-the-upload";

    /// <summary>
    /// A result with a chunk on pages 1 to 2 under two headings, a chunk with headings only, a blank chunk that the
    /// converter drops, and 3 pages in the converted document.
    /// </summary>
    public const string ThreePageResult = """
        {
          "chunks": [
            {
              "text": "Operating limits.",
              "headings": ["Manual", "Limits"],
              "page_numbers": [2, 1, 2]
            },
            {
              "text": "Maximum pressure is 6 bar.",
              "headings": ["Manual", "Pressure"],
              "page_numbers": []
            },
            { "text": "   ", "headings": [], "page_numbers": [3] }
          ],
          "documents": [
            { "content": { "json_content": { "pages": { "1": {}, "2": {}, "3": {} } } } }
          ]
        }
        """;

    public const string NoPagesResult = """
        {
          "chunks": [ { "text": "Plain text.", "headings": [], "page_numbers": [] } ],
          "documents": [ { "content": { "json_content": { "pages": {} } } } ]
        }
        """;

    public const string SlideResult = """
        {
          "chunks": [
            { "text": "Slide text.", "headings": ["Deck"], "page_numbers": [4] }
          ],
          "documents": [
            { "content": { "json_content": { "pages": { "4": {} } } } }
          ]
        }
        """;
}
