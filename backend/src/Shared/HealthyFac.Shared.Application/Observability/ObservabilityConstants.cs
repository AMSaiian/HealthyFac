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
    /// The names of the Document file handling. The source is named after the .NET namespace of the file code and is
    /// registered by the Document service itself.
    /// </summary>
    public static class Files
    {
        public const string Source = "HealthyFac.Document.Files";

        public static class Spans
        {
            public const string Inspect = "File.Inspect";
            public const string BlobUpload = "Blob.Upload";
        }

        public static class Tags
        {
            /// <summary>The stored extension of the detected type, or <see cref="Values.UnknownType"/>.</summary>
            public const string DetectedType = "healthyfac.file.detected_type";

            /// <summary>The purpose of the upload (<c>attachment</c> or <c>embedded</c>).</summary>
            public const string Purpose = "healthyfac.file.purpose";

            /// <summary>The result of the inspection, from <see cref="Values"/>.</summary>
            public const string Result = "healthyfac.file.result";
        }

        public static class Values
        {
            public const string UnknownType = "unknown";
            public const string Accepted = "accepted";
            public const string Rejected = "rejected";
        }
    }

    /// <summary>
    /// The names of the Telemetry MQTT intake. The source and the meter are named after the .NET namespace of the
    /// intake and are registered by the Telemetry service itself.
    /// </summary>
    public static class Intake
    {
        public const string Source = "HealthyFac.Telemetry.Intake";
        public const string Meter = Source;

        /// <summary>The value of <see cref="Messaging.Tags.SystemName"/> for the MQTT broker.</summary>
        public const string MqttSystem = "mqtt";

        /// <summary>The subscription pattern. It holds no gateway ID, so a span name stays fixed.</summary>
        public const string DestinationPattern = "healthyfac/gateways/+/messages";

        public const string SpanName = $"{Messaging.Operations.Process} {DestinationPattern}";

        public static class Tags
        {
            /// <summary>The shared subscription group of the consumers.</summary>
            public const string ConsumerGroup = "messaging.consumer.group.name";

            public const string ConsumerIndex = "healthyfac.intake.consumer_index";

            /// <summary>The result of a message, from <see cref="Results"/>.</summary>
            public const string Result = "healthyfac.intake.result";

            /// <summary>The envelope error code of a rejected message. The set of codes is small and fixed.</summary>
            public const string ErrorCode = "healthyfac.intake.error_code";
        }

        public static class Results
        {
            public const string Stored = "stored";
            public const string StoredWithErrors = "stored_with_errors";
            public const string Rejected = "rejected";
            public const string Duplicate = "duplicate";
            public const string Invalid = "invalid";
            public const string Failed = "failed";
            public const string Redelivery = "redelivery";
        }

        public static class Instruments
        {
            public const string Messages = "healthyfac.intake.messages";
            public const string HandleDuration = "healthyfac.intake.handle.duration";
            public const string Lag = "healthyfac.intake.lag";
            public const string InFlight = "healthyfac.intake.in_flight";
            public const string ConsumersConnected = "healthyfac.intake.consumers.connected";
            public const string OrphanEmptied = "healthyfac.intake.orphan.emptied";
            public const string DrainTimeouts = "healthyfac.intake.drain.timeouts";
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
        public const string Sessions = "{session}";
    }

    public static class Outcomes
    {
        public const string Success = "success";
        public const string Rejected = "rejected";
        public const string Failure = "failure";
    }
}
