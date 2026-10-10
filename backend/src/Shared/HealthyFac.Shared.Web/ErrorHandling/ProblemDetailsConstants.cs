namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Defines problem details titles and type URLs, grouped by the error each one reports.
/// </summary>
public static class ProblemDetailsConstants
{
    public static class BadRequest
    {
        public const string Title = "Bad Request";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        public const string GenericDetail = "The request could not be read. Check the payload and headers.";
    }

    public static class Validation
    {
        public const string Title = "Validation Error";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    }

    public static class NotFound
    {
        public const string Title = "Resource Not Found";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
    }

    public static class Unauthorized
    {
        public const string Title = "Unauthorized";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7235#section-3.1";
    }

    public static class Forbidden
    {
        public const string Title = "Forbidden";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
    }

    public static class Conflict
    {
        public const string Title = "Conflict";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
    }

    public static class Unprocessable
    {
        public const string Title = "Unprocessable Entity";
        public const string TypeUrl = "https://www.rfc-editor.org/rfc/rfc4918#section-11.2";
    }

    public static class InternalServerError
    {
        public const string Title = "Unhandled exception occurred";
        public const string TypeUrl = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        public const string GenericDetail = "An unexpected error occurred. Contact support if it persists.";
    }
}
