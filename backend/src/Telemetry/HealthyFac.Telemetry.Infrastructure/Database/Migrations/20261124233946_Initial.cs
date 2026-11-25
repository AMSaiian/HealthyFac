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
                name: "draft_proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_proposals", x => x.id);
                    table.CheckConstraint("ck_draft_proposal_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_draft_proposal_source_allowed", "\"source\" IN ('chat', 'ingest')");
                    table.CheckConstraint("ck_draft_proposals_source_fields", "(source = 'ingest' AND attempt_id IS NOT NULL AND file_id IS NOT NULL AND chat_id IS NULL AND chat_message_id IS NULL) OR (source = 'chat' AND chat_id IS NOT NULL AND chat_message_id IS NOT NULL AND attempt_id IS NULL AND file_id IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
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
                    company_id = table.Column<Guid>(type: "uuid", nullable: true)
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
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    gateway_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, collation: "case_insensitive"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_schema_revision = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    state_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expected_go_live_on = table.Column<DateOnly>(type: "date", nullable: false),
                    go_live_starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    go_live_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_registrations", x => x.id);
                    table.CheckConstraint("ck_machine_registration_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_registration_gateway_id_length", "char_length(\"gateway_id\") BETWEEN 16 AND 16");
                    table.CheckConstraint("ck_machine_registration_state_allowed", "\"state\" IN ('inMaintenance', 'operational', 'outOfService')");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, collation: "case_insensitive"),
                    trace_parent = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_message_actor_id_not_empty", "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "proposal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_entries", x => x.id);
                    table.CheckConstraint("ck_proposal_entries_machine_only_on_machine", "(machine_id IS NOT NULL) = (state = 'onMachine')");
                    table.CheckConstraint("ck_proposal_entry_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_proposal_entry_state_allowed", "\"state\" IN ('awaiting', 'machineDraftDismissed', 'onMachine', 'onMachineDraft', 'rejected')");
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
                name: "metric_definition_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    source_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "case_insensitive"),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    value_kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, collation: "case_insensitive"),
                    quantity_kind_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    min = table.Column<double>(type: "double precision", nullable: true),
                    max = table.Column<double>(type: "double precision", nullable: true),
                    states = table.Column<string[]>(type: "text[]", nullable: true),
                    warning_low = table.Column<double>(type: "double precision", nullable: true),
                    warning_high = table.Column<double>(type: "double precision", nullable: true),
                    alarm_low = table.Column<double>(type: "double precision", nullable: true),
                    alarm_high = table.Column<double>(type: "double precision", nullable: true),
                    behavior = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_definition_drafts", x => x.id);
                    table.CheckConstraint("ck_metric_definition_draft_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_metric_definition_draft_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_metric_definition_draft_key_length", "char_length(\"key\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_metric_definition_draft_name_length", "char_length(\"name\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_metric_definition_draft_proposal_entry_id_not_empty", "\"proposal_entry_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_metric_definition_draft_proposal_id_not_empty", "\"proposal_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_metric_definition_draft_source_location_length", "char_length(\"source_location\") BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_metric_definition_draft_state_allowed", "\"state\" IN ('applied', 'awaitingTarget', 'dismissed', 'pending')");
                    table.CheckConstraint("ck_metric_definition_drafts_behavior_allowed", "\"behavior\" IN ('counter', 'level')");
                    table.CheckConstraint("ck_metric_definition_drafts_min_below_max", "min IS NULL OR max IS NULL OR min < max");
                    table.CheckConstraint("ck_metric_definition_drafts_target", "(state IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) <= 1) OR (state NOT IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) = 1)");
                    table.CheckConstraint("ck_metric_definition_drafts_value_kind_allowed", "\"value_kind\" IN ('boolean', 'numeric', 'stateSet')");
                    table.CheckConstraint("ck_metric_definition_drafts_warning_high_not_above_alarm_high", "warning_high IS NULL OR alarm_high IS NULL OR warning_high <= alarm_high");
                    table.CheckConstraint("ck_metric_definition_drafts_warning_low_not_below_alarm_low", "warning_low IS NULL OR alarm_low IS NULL OR warning_low >= alarm_low");
                    table.ForeignKey(
                        name: "FK_metric_definition_drafts_draft_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "draft_proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operating_phase_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    source_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "case_insensitive"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, collation: "case_insensitive"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    priority = table.Column<int>(type: "integer", nullable: true),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phase_drafts", x => x.id);
                    table.CheckConstraint("ck_operating_phase_draft_code_length", "char_length(\"code\") BETWEEN 1 AND 50");
                    table.CheckConstraint("ck_operating_phase_draft_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_operating_phase_draft_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_operating_phase_draft_name_length", "char_length(\"name\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_operating_phase_draft_proposal_entry_id_not_empty", "\"proposal_entry_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_operating_phase_draft_proposal_id_not_empty", "\"proposal_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_operating_phase_draft_source_location_length", "char_length(\"source_location\") BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_operating_phase_draft_state_allowed", "\"state\" IN ('applied', 'awaitingTarget', 'dismissed', 'pending')");
                    table.CheckConstraint("ck_operating_phase_drafts_target", "(state IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) <= 1) OR (state NOT IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) = 1)");
                    table.ForeignKey(
                        name: "FK_operating_phase_drafts_draft_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "draft_proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "status_definition_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    source_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "case_insensitive"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, collation: "case_insensitive"),
                    severity = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true, collation: "case_insensitive"),
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
                    table.PrimaryKey("PK_status_definition_drafts", x => x.id);
                    table.CheckConstraint("ck_status_definition_draft_code_length", "char_length(\"code\") BETWEEN 1 AND 50");
                    table.CheckConstraint("ck_status_definition_draft_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_status_definition_draft_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_status_definition_draft_proposal_entry_id_not_empty", "\"proposal_entry_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_status_definition_draft_proposal_id_not_empty", "\"proposal_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_status_definition_draft_source_location_length", "char_length(\"source_location\") BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_status_definition_draft_state_allowed", "\"state\" IN ('applied', 'awaitingTarget', 'dismissed', 'pending')");
                    table.CheckConstraint("ck_status_definition_drafts_severity_allowed", "\"severity\" IN ('error', 'info', 'warning')");
                    table.CheckConstraint("ck_status_definition_drafts_target", "(state IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) <= 1) OR (state NOT IN ('awaitingTarget', 'dismissed') AND num_nonnulls(target_machine_id, target_machine_draft_id) = 1)");
                    table.ForeignKey(
                        name: "FK_status_definition_drafts_draft_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "draft_proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    watermark = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_phase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mismatch_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    switched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unmatched_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_evaluations_machine_registrations_id",
                        column: x => x.id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "operating_phases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phases", x => x.id);
                    table.CheckConstraint("ck_operating_phase_code_not_blank", "\"code\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_operating_phase_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_operating_phase_machine_registration_id_not_empty", "\"machine_registration_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_operating_phase_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.ForeignKey(
                        name: "FK_operating_phases_machine_registrations_machine_registration~",
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
                name: "rule_streaks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    open_count = table.Column<int>(type: "integer", nullable: false),
                    escalate_count = table.Column<int>(type: "integer", nullable: false),
                    resolve_count = table.Column<int>(type: "integer", nullable: false),
                    open_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolve_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    counts_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_streaks", x => x.id);
                    table.CheckConstraint("ck_rule_streak_rule_allowed", "\"rule\" IN ('drift', 'silentMachine', 'status', 'threshold', 'trend', 'unknownMode')");
                    table.ForeignKey(
                        name: "FK_rule_streaks_machine_registrations_machine_registration_id",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "signal_episodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    severity = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    state = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    operating_phase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_episodes", x => x.id);
                    table.CheckConstraint("ck_signal_episode_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_signal_episode_rule_allowed", "\"rule\" IN ('drift', 'silentMachine', 'status', 'threshold', 'trend', 'unknownMode')");
                    table.CheckConstraint("ck_signal_episode_severity_allowed", "\"severity\" IN ('alarm', 'warning')");
                    table.CheckConstraint("ck_signal_episode_state_allowed", "\"state\" IN ('open', 'resolved')");
                    table.ForeignKey(
                        name: "FK_signal_episodes_machine_registrations_machine_registration_~",
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
                name: "operating_phase_draft_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    @operator = table.Column<string>(name: "operator", type: "text", nullable: true, collation: "case_insensitive"),
                    value = table.Column<double>(type: "double precision", nullable: true),
                    upper_value = table.Column<double>(type: "double precision", nullable: true),
                    states = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phase_draft_conditions", x => x.id);
                    table.CheckConstraint("ck_operating_phase_draft_conditions_exactly_one_source", "num_nonnulls(metric_definition_id, metric_draft_id, status_definition_id, status_draft_id) = 1");
                    table.ForeignKey(
                        name: "FK_operating_phase_draft_conditions_operating_phase_drafts_dra~",
                        column: x => x.draft_id,
                        principalTable: "operating_phase_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operating_phase_draft_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warning_low = table.Column<double>(type: "double precision", nullable: true),
                    warning_high = table.Column<double>(type: "double precision", nullable: true),
                    alarm_low = table.Column<double>(type: "double precision", nullable: true),
                    alarm_high = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phase_draft_limits", x => x.id);
                    table.CheckConstraint("ck_operating_phase_draft_limits_exactly_one_metric", "num_nonnulls(metric_definition_id, metric_draft_id) = 1");
                    table.ForeignKey(
                        name: "FK_operating_phase_draft_limits_operating_phase_drafts_draft_id",
                        column: x => x.draft_id,
                        principalTable: "operating_phase_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phase_intervals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_phase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phase_intervals", x => x.id);
                    table.CheckConstraint("ck_phase_intervals_ended_at_not_before_started_at", "ended_at IS NULL OR ended_at >= started_at");
                    table.ForeignKey(
                        name: "FK_phase_intervals_machine_registrations_machine_registration_~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phase_intervals_operating_phases_operating_phase_id",
                        column: x => x.operating_phase_id,
                        principalTable: "operating_phases",
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
                    behavior = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValueSql: "'level'"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    alarm_high = table.Column<double>(type: "double precision", nullable: true),
                    alarm_low = table.Column<double>(type: "double precision", nullable: true),
                    warning_high = table.Column<double>(type: "double precision", nullable: true),
                    warning_low = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_definitions", x => x.id);
                    table.CheckConstraint("ck_metric_definition_behavior_allowed", "\"behavior\" IN ('counter', 'level')");
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
                name: "metric_statistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operating_phase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warm_count = table.Column<int>(type: "integer", nullable: false),
                    warm_mean = table.Column<double>(type: "double precision", nullable: false),
                    warm_m2 = table.Column<double>(type: "double precision", nullable: false),
                    is_warm = table.Column<bool>(type: "boolean", nullable: false),
                    slow_mean = table.Column<double>(type: "double precision", nullable: false),
                    variance = table.Column<double>(type: "double precision", nullable: false),
                    mean_interval_seconds = table.Column<double>(type: "double precision", nullable: true),
                    drift_spread = table.Column<double>(type: "double precision", nullable: false),
                    drift_spread_count = table.Column<int>(type: "integer", nullable: false),
                    fast_mean = table.Column<double>(type: "double precision", nullable: false),
                    level = table.Column<double>(type: "double precision", nullable: false),
                    slope = table.Column<double>(type: "double precision", nullable: false),
                    last_sampled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    samples_since_restart = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_statistics", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_statistics_metric_definitions_metric_definition_id",
                        column: x => x.metric_definition_id,
                        principalTable: "metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metric_statistics_operating_phases_operating_phase_id",
                        column: x => x.operating_phase_id,
                        principalTable: "operating_phases",
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

            migrationBuilder.CreateTable(
                name: "operating_phase_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    @operator = table.Column<string>(name: "operator", type: "character varying(14)", maxLength: 14, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: true),
                    upper_value = table.Column<double>(type: "double precision", nullable: true),
                    states = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    phase_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phase_conditions", x => x.id);
                    table.CheckConstraint("ck_operating_phase_condition_exactly_one_source", "(metric_definition_id IS NOT NULL) <> (status_definition_id IS NOT NULL)");
                    table.CheckConstraint("ck_operating_phase_condition_operator_allowed", "\"operator\" IN ('active', 'between', 'greaterOrEqual', 'greaterThan', 'in', 'inactive', 'isFalse', 'isTrue', 'lessOrEqual', 'lessThan')");
                    table.ForeignKey(
                        name: "FK_operating_phase_conditions_metric_definitions_metric_defini~",
                        column: x => x.metric_definition_id,
                        principalTable: "metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operating_phase_conditions_operating_phases_phase_id",
                        column: x => x.phase_id,
                        principalTable: "operating_phases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_operating_phase_conditions_status_definitions_status_defini~",
                        column: x => x.status_definition_id,
                        principalTable: "status_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operating_phase_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warning_low = table.Column<double>(type: "double precision", nullable: true),
                    warning_high = table.Column<double>(type: "double precision", nullable: true),
                    alarm_low = table.Column<double>(type: "double precision", nullable: true),
                    alarm_high = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    phase_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operating_phase_limits", x => x.id);
                    table.ForeignKey(
                        name: "FK_operating_phase_limits_metric_definitions_metric_definition~",
                        column: x => x.metric_definition_id,
                        principalTable: "metric_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operating_phase_limits_operating_phases_phase_id",
                        column: x => x.phase_id,
                        principalTable: "operating_phases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_draft_proposals_chat_message_id",
                table: "draft_proposals",
                column: "chat_message_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_draft_proposals_company_id_attempt_id",
                table: "draft_proposals",
                columns: new[] { "company_id", "attempt_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_draft_proposals_company_id_chat_id",
                table: "draft_proposals",
                columns: new[] { "company_id", "chat_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_machine_evaluations_due_at",
                table: "machine_evaluations",
                column: "due_at");

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
                name: "ix_metric_definition_drafts_company_id_proposal_id",
                table: "metric_definition_drafts",
                columns: new[] { "company_id", "proposal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_metric_definition_drafts_company_id_target_machine_id_state",
                table: "metric_definition_drafts",
                columns: new[] { "company_id", "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_metric_definition_drafts_proposal_entry_id",
                table: "metric_definition_drafts",
                column: "proposal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_definition_drafts_proposal_id",
                table: "metric_definition_drafts",
                column: "proposal_id");

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
                name: "IX_metric_statistics_operating_phase_id",
                table: "metric_statistics",
                column: "operating_phase_id");

            migrationBuilder.CreateIndex(
                name: "ux_metric_statistics_metric_definition_id_operating_phase_id",
                table: "metric_statistics",
                columns: new[] { "metric_definition_id", "operating_phase_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_conditions_metric_definition_id",
                table: "operating_phase_conditions",
                column: "metric_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_conditions_phase_id",
                table: "operating_phase_conditions",
                column: "phase_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_conditions_status_definition_id",
                table: "operating_phase_conditions",
                column: "status_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_draft_conditions_draft_id",
                table: "operating_phase_draft_conditions",
                column: "draft_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_draft_limits_draft_id",
                table: "operating_phase_draft_limits",
                column: "draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_operating_phase_drafts_company_id_proposal_id",
                table: "operating_phase_drafts",
                columns: new[] { "company_id", "proposal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_operating_phase_drafts_company_id_target_machine_id_state",
                table: "operating_phase_drafts",
                columns: new[] { "company_id", "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_operating_phase_drafts_proposal_entry_id",
                table: "operating_phase_drafts",
                column: "proposal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_drafts_proposal_id",
                table: "operating_phase_drafts",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_limits_metric_definition_id",
                table: "operating_phase_limits",
                column: "metric_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_operating_phase_limits_phase_id_metric_definition_id",
                table: "operating_phase_limits",
                columns: new[] { "phase_id", "metric_definition_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_operating_phases_machine_registration_id_code",
                table: "operating_phases",
                columns: new[] { "machine_registration_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_operating_phases_machine_registration_id_priority",
                table: "operating_phases",
                columns: new[] { "machine_registration_id", "priority" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_phase_intervals_machine_registration_id_started_at",
                table: "phase_intervals",
                columns: new[] { "machine_registration_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_phase_intervals_operating_phase_id",
                table: "phase_intervals",
                column: "operating_phase_id");

            migrationBuilder.CreateIndex(
                name: "ux_phase_intervals_machine_registration_id_open",
                table: "phase_intervals",
                column: "machine_registration_id",
                unique: true,
                filter: "ended_at IS NULL");

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
                name: "ux_rule_streaks_machine_registration_id_rule_subject_id",
                table: "rule_streaks",
                columns: new[] { "machine_registration_id", "rule", "subject_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_signal_episodes_machine_registration_id_opened_at",
                table: "signal_episodes",
                columns: new[] { "machine_registration_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ux_signal_episodes_open_machine_registration_id_rule_subject",
                table: "signal_episodes",
                columns: new[] { "machine_registration_id", "rule", "metric_definition_id", "status_definition_id" },
                unique: true,
                filter: "state = 'open'")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_status_definition_drafts_company_id_proposal_id",
                table: "status_definition_drafts",
                columns: new[] { "company_id", "proposal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_status_definition_drafts_company_id_target_machine_id_state",
                table: "status_definition_drafts",
                columns: new[] { "company_id", "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_status_definition_drafts_proposal_entry_id",
                table: "status_definition_drafts",
                column: "proposal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_status_definition_drafts_proposal_id",
                table: "status_definition_drafts",
                column: "proposal_id");

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
                name: "machine_evaluations");

            migrationBuilder.DropTable(
                name: "metric_definition_drafts");

            migrationBuilder.DropTable(
                name: "metric_schema_revisions");

            migrationBuilder.DropTable(
                name: "metric_statistics");

            migrationBuilder.DropTable(
                name: "numeric_readings");

            migrationBuilder.DropTable(
                name: "operating_phase_conditions");

            migrationBuilder.DropTable(
                name: "operating_phase_draft_conditions");

            migrationBuilder.DropTable(
                name: "operating_phase_draft_limits");

            migrationBuilder.DropTable(
                name: "operating_phase_limits");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "phase_intervals");

            migrationBuilder.DropTable(
                name: "proposal_entries");

            migrationBuilder.DropTable(
                name: "received_messages");

            migrationBuilder.DropTable(
                name: "rule_streaks");

            migrationBuilder.DropTable(
                name: "signal_episodes");

            migrationBuilder.DropTable(
                name: "status_definition_drafts");

            migrationBuilder.DropTable(
                name: "status_readings");

            migrationBuilder.DropTable(
                name: "operating_phase_drafts");

            migrationBuilder.DropTable(
                name: "metric_definitions");

            migrationBuilder.DropTable(
                name: "operating_phases");

            migrationBuilder.DropTable(
                name: "status_definitions");

            migrationBuilder.DropTable(
                name: "draft_proposals");

            migrationBuilder.DropTable(
                name: "measurement_units");

            migrationBuilder.DropTable(
                name: "machine_registrations");

            migrationBuilder.DropTable(
                name: "quantity_kinds");
        }
    }
}
