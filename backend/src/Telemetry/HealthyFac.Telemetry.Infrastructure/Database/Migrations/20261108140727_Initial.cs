using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, collation: "case_insensitive"),
                    trace_parent = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.id);
                    table.CheckConstraint("ck_inbox_message_actor_id_not_empty", "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_inbox_message_id_not_empty", "\"id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "machine_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, collation: "case_insensitive"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_schema_revision = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_registrations", x => x.id);
                    table.CheckConstraint("ck_machine_registration_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_registration_gateway_id_length", "char_length(\"gateway_id\") BETWEEN 16 AND 16");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, collation: "case_insensitive"),
                    trace_parent = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_message_actor_id_not_empty", "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "quantity_kinds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quantity_kinds", x => x.id);
                    table.CheckConstraint("ck_quantity_kind_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                });

            migrationBuilder.CreateTable(
                name: "metric_schema_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    schema = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_schema_revisions", x => x.id);
                    table.CheckConstraint("ck_metric_schema_revision_machine_registration_id_not_empty", "\"machine_registration_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_metric_schema_revision_revision_range", "\"revision\" BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_metric_schema_revisions_machine_registrations_machine_regis~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "received_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    topic = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false, collation: "case_insensitive"),
                    sampled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    schema_revision = table.Column<int>(type: "integer", nullable: true),
                    raw_payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    errors = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_received_messages", x => x.id);
                    table.CheckConstraint("ck_received_message_exactly_one_payload_form", "(payload IS NULL) <> (raw_payload IS NULL)");
                    table.ForeignKey(
                        name: "FK_received_messages_machine_registrations_machine_registratio~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "status_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    severity = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_definitions", x => x.id);
                    table.CheckConstraint("ck_status_definition_code_not_blank", "\"code\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_status_definition_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_status_definition_machine_registration_id_not_empty", "\"machine_registration_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_status_definition_severity_allowed", "\"severity\" IN ('error', 'info', 'warning')");
                    table.ForeignKey(
                        name: "FK_status_definitions_machine_registrations_machine_registrati~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "measurement_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_kind_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    abbreviation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement_units", x => x.id);
                    table.CheckConstraint("ck_measurement_unit_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_measurement_unit_quantity_kind_id_not_empty", "\"quantity_kind_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_measurement_units_quantity_kinds_quantity_kind_id",
                        column: x => x.quantity_kind_id,
                        principalTable: "quantity_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "status_readings",
                columns: table => new
                {
                    status_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_readings", x => new { x.status_definition_id, x.sampled_at });
                    table.ForeignKey(
                        name: "FK_status_readings_status_definitions_status_definition_id",
                        column: x => x.status_definition_id,
                        principalTable: "status_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metric_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    value_kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    measurement_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min = table.Column<double>(type: "double precision", nullable: true),
                    max = table.Column<double>(type: "double precision", nullable: true),
                    states = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_definitions", x => x.id);
                    table.CheckConstraint("ck_metric_definition_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_metric_definition_key_length", "char_length(\"key\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_metric_definition_key_not_blank", "\"key\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_metric_definition_machine_registration_id_not_empty", "\"machine_registration_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_metric_definition_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_metric_definition_value_kind_allowed", "\"value_kind\" IN ('boolean', 'numeric', 'stateSet')");
                    table.ForeignKey(
                        name: "FK_metric_definitions_machine_registrations_machine_registrati~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metric_definitions_measurement_units_measurement_unit_id",
                        column: x => x.measurement_unit_id,
                        principalTable: "measurement_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discrete_readings",
                columns: table => new
                {
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    is_out_of_range = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discrete_readings", x => new { x.metric_definition_id, x.sampled_at });
                    table.ForeignKey(
                        name: "FK_discrete_readings_metric_definitions_metric_definition_id",
                        column: x => x.metric_definition_id,
                        principalTable: "metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "numeric_readings",
                columns: table => new
                {
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    is_out_of_range = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_numeric_readings", x => new { x.metric_definition_id, x.sampled_at });
                    table.ForeignKey(
                        name: "FK_numeric_readings_metric_definitions_metric_definition_id",
                        column: x => x.metric_definition_id,
                        principalTable: "metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_machine_registrations_gateway_id",
                table: "machine_registrations",
                column: "gateway_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_measurement_units_quantity_kind_id_name",
                table: "measurement_units",
                columns: new[] { "quantity_kind_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_metric_definitions_machine_registration_id_key",
                table: "metric_definitions",
                columns: new[] { "machine_registration_id", "key" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_metric_definitions_measurement_unit_id",
                table: "metric_definitions",
                column: "measurement_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_schema_revisions_machine_registration_id_revision",
                table: "metric_schema_revisions",
                columns: new[] { "machine_registration_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quantity_kinds_name",
                table: "quantity_kinds",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_received_messages_machine_registration_id_sampled_at",
                table: "received_messages",
                columns: new[] { "machine_registration_id", "sampled_at" },
                unique: true,
                filter: "machine_registration_id IS NOT NULL AND sampled_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_status_definitions_machine_registration_id_code",
                table: "status_definitions",
                columns: new[] { "machine_registration_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discrete_readings");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "metric_schema_revisions");

            migrationBuilder.DropTable(
                name: "numeric_readings");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "received_messages");

            migrationBuilder.DropTable(
                name: "status_readings");

            migrationBuilder.DropTable(
                name: "metric_definitions");

            migrationBuilder.DropTable(
                name: "status_definitions");

            migrationBuilder.DropTable(
                name: "measurement_units");

            migrationBuilder.DropTable(
                name: "machine_registrations");

            migrationBuilder.DropTable(
                name: "quantity_kinds");
        }
    }
}
