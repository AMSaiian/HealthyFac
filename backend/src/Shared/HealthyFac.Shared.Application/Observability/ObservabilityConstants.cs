namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Defines activity source names, span names, and tag keys for use case, messaging, rendering, and email tracing.
/// </summary>
public static class ObservabilityConstants
{
    public static class Sources
    {
        public const string UseCases = "HealthyFac.UseCases";
        public const string IdEncryption = "HealthyFac.IdEncryption";
        public const string Messaging = "HealthyFac.Messaging";
        public const string Rendering = "HealthyFac.Rendering";
        public const string Email = "HealthyFac.Email";
    }

    public static class Spans
    {
        public const string Validation = "Validation";
        public const string Encrypt = "IdEncryption.Encrypt";
        public const string Decrypt = "IdEncryption.Decrypt";
        public const string RazorRender = "Razor.Render";
        public const string EmailSend = "Email.Send";
    }

    public static class Tags
    {
        public const string UseCase = "healthyfac.use_case";
        public const string Outcome = "healthyfac.outcome";
        public const string ValidatorCount = "healthyfac.validator_count";
        public const string RejectionCount = "healthyfac.rejection_count";
    }

    public static class Rendering
    {
        public static class Tags
        {
            /// <summary>The type name of the rendered component.</summary>
            public const string Component = "healthyfac.rendering.component";

            /// <summary>The output format, from <see cref="Formats"/>.</summary>
            public const string Format = "healthyfac.rendering.format";
        }

        public static class Formats
        {
            public const string Html = "html";
            public const string Text = "text";
        }
    }

    /// <summary>
    /// Follows the OpenTelemetry messaging semantic conventions. No referenced package defines these names, and the
    /// <c>OpenTelemetry.SemanticConventions</c> package has no stable release.
    /// </summary>
    public static class Messaging
    {
        /// <summary>The value of <see cref="Tags.SystemName"/> for the RabbitMQ broker.</summary>
        public const string RabbitMq = "rabbitmq";

        public static class Tags
        {
            public const string SystemName = "messaging.system";
            public const string OperationType = "messaging.operation.type";
            public const string DestinationName = "messaging.destination.name";
            public const string MessageId = "messaging.message.id";

            /// <summary>The number of failed attempts of the inbox row before the current one.</summary>
            public const string AttemptCount = "healthyfac.attempt_count";
        }

        public static class Operations
        {
            public const string Send = "send";
            public const string Receive = "receive";
            public const string Process = "process";
        }
    }

    public static class Outcomes
    {
        public const string Success = "success";
        public const string Rejected = "rejected";
        public const string Failure = "failure";
    }
}
