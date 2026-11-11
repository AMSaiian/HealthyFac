namespace HealthyFac.Insights.Infrastructure.Conversion;

/// <summary>
/// Defines the endpoint paths, the form field names, and the values of the docling-serve API (version 1.38.2).
/// </summary>
public static class DoclingConstants
{
    public const string ClientName = "docling";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string HealthPath = "/health";
    public const string SubmitPath = "/v1/chunk/hybrid/file/async";
    public const string PollPathFormat = "/v1/status/poll/{0}";
    public const string ResultPathFormat = "/v1/result/{0}";
    public const string FilePartName = "files";

    public const string OcrEngine = "easyocr";
    public const int ChunkMaxTokens = 800;

    public static class Fields
    {
        public const string IncludeConvertedDocument = "include_converted_doc";
        public const string DoOcr = "convert_do_ocr";
        public const string OcrEngine = "convert_ocr_engine";
        public const string DoTableStructure = "convert_do_table_structure";
        public const string PageRange = "convert_page_range";
        public const string ChunkMaxTokens = "chunking_max_tokens";
        public const string ChunkMergePeers = "chunking_merge_peers";
    }

    public static class Status
    {
        public const string Success = "success";
        public const string Failure = "failure";
    }

    public static class Location
    {
        public const string HeadingSeparator = " > ";
        public const string PartSeparator = ": ";
        public const string PageRangeSeparator = "-";
        public const string PageLabel = "page";
        public const string SlideLabel = "slide";
        public const string SheetLabel = "sheet";
        public const string SlideExtension = "pptx";
        public const string SheetExtension = "xlsx";
    }
}
