namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Defines the resource attributes, activity source names, span names, and tag keys of the shared telemetry.
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

    /// <summary>
    /// Follows the OpenTelemetry resource semantic conventions. The SDK keeps its own names internal, and the
    /// <c>OpenTelemetry.SemanticConventions</c> package has no stable release.
    /// </summary>
    public static class Resource
    {
        /// <summary>The value of <see cref="Attributes.ServiceNamespace"/> for every HealthyFac service.</summary>
        public const string Namespace = "healthyfac";

        public static class Attributes
        {
            public const string ServiceNamespace = "service.namespace";
            public const string ServiceName = "service.name";
            public const string ServiceVersion = "service.version";
            public const string ServiceInstanceId = "service.instance.id";
            public const string DeploymentEnvironment = "deployment.environment.name";
        }
    }

    public static class Spans
    {
        public const string Validation = "UseCase.Validate";
        public const string PreHandlerCheck = "UseCase.PreHandlerCheck";
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

            /// <summary>The processor of a metric, from <see cref="Processors"/>.</summary>
            public const string Processor = "healthyfac.messaging.processor";

            /// <summary>The stable contract name of the message. The set of names is small and fixed.</summary>
            public const string MessageType = "healthyfac.messaging.message_type";

            /// <summary>The reason of a final failure, from <see cref="Failures"/>.</summary>
            public const string Failure = "healthyfac.messaging.failure";
        }

        public static class Operations
        {
            public const string Send = "send";
            public const string Receive = "receive";
            public const string Process = "process";
        }

        public static class Processors
        {
            public const string Outbox = "outbox";
            public const string Inbox = "inbox";
        }

        public static class Failures
        {
            /// <summary>
            /// No retry can fix the message (e.g., an invalid payload), so it failed at the first attempt.
            /// </summary>
            public const string Permanent = "permanent";

            /// <summary>The message failed at the last allowed attempt.</summary>
            public const string AttemptLimit = "attempt_limit";
        }

        /// <summary>
        /// The instrument names of the <see cref="Meters.Messaging"/> meter. Each name holds no unit; the
        /// instrument declares it (<see cref="Units"/>).
        /// </summary>
        public static class Instruments
        {
            public const string Published = "healthyfac.messaging.published";
            public const string Handled = "healthyfac.messaging.handled";
            public const string Retried = "healthyfac.messaging.retried";
            public const string Failed = "healthyfac.messaging.failed";
            public const string RunDuration = "healthyfac.messaging.run.duration";
            public const string HandleDuration = "healthyfac.messaging.handle.duration";
            public const string Pending = "healthyfac.messaging.pending";
            public const string OldestPendingAge = "healthyfac.messaging.oldest_pending.age";
        }
    }

    /// <summary>
    /// The meter names. A meter is named after its .NET namespace, like an activity source. The built-in meters
    /// of ASP.NET Core and <c>HttpClient</c> come with the instrumentation packages, so only the others are here.
    /// </summary>
    public static class Meters
    {
        public const string Messaging = Sources.Messaging;

        /// <summary>The built-in .NET runtime meter (GC, thread pool, exceptions).</summary>
        public const string Runtime = "System.Runtime";
    }

    /// <summary>The UCUM units of the instruments.</summary>
    public static class Units
    {
        public const string Seconds = "s";
        public const string Messages = "{message}";
    }

    public static class Outcomes
    {
        public const string Success = "success";
        public const string Rejected = "rejected";
        public const string Failure = "failure";
    }
}
