namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

public static class ReceivedMessageConstraintTestData
{
    private const string InsertPrefix =
        "INSERT INTO received_messages (id, topic, received_at, raw_payload, payload, errors) VALUES ";

    public static TheoryData<string> InvalidPayloadForms => new()
    {
        InsertPrefix + "(gen_random_uuid(), 't', now(), '\x7b7d'::bytea, '[]'::jsonb, '[]'::jsonb)",
        InsertPrefix + "(gen_random_uuid(), 't', now(), NULL, NULL, '[]'::jsonb)",
    };
}
