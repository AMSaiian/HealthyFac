using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace HealthyFac.Insights.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, collation: "case_insensitive"),
                    state = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    summary_through_sequence = table.Column<int>(type: "integer", nullable: true),
                    is_summary_stale = table.Column<bool>(type: "boolean", nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_sequence = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chats", x => x.id);
                    table.CheckConstraint("ck_chat_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_next_sequence_range", "\"next_sequence\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_chat_owner_user_id_not_empty", "\"owner_user_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_state_allowed", "\"state\" IN ('blocked', 'open')");
                    table.CheckConstraint("ck_chat_title_length", "char_length(\"title\") BETWEEN 0 AND 80");
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
                name: "ingest_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    extension = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, collation: "case_insensitive"),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    state = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    error_code = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    error_detail = table.Column<string>(type: "jsonb", nullable: true),
                    interrupt_reason = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    warning = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "case_insensitive"),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trace_parent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true, collation: "case_insensitive"),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    chunk_count = table.Column<int>(type: "integer", nullable: true),
                    proposed_draft_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingest_attempts", x => x.id);
                    table.CheckConstraint("ck_ingest_attempt_chunk_count_not_negative", "chunk_count IS NULL OR chunk_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingest_attempt_error_code_allowed", "\"error_code\" IN ('conversionFailed', 'conversionTimeout', 'fileNotFound', 'invalidExtraction', 'noText', 'providerUnavailable', 'tooManyPages', 'unsafeContent')");
                    table.CheckConstraint("ck_ingest_attempt_error_code_only_when_failed", "error_code IS NULL OR state = 'failed'");
                    table.CheckConstraint("ck_ingest_attempt_extension_not_blank", "\"extension\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingest_attempt_file_id_not_empty", "\"file_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingest_attempt_file_name_not_blank", "\"file_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingest_attempt_finished_at_only_when_final", "finished_at IS NULL OR state NOT IN ('queued', 'inProcess')");
                    table.CheckConstraint("ck_ingest_attempt_interrupt_reason_allowed", "\"interrupt_reason\" IN ('cancelled', 'fileDeleted', 'leaseExpired', 'serviceStopped')");
                    table.CheckConstraint("ck_ingest_attempt_interrupt_reason_only_when_interrupted", "interrupt_reason IS NULL OR state = 'interrupted'");
                    table.CheckConstraint("ck_ingest_attempt_page_count_not_negative", "page_count IS NULL OR page_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_proposed_draft_count_not_negative", "proposed_draft_count IS NULL OR proposed_draft_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_size_bytes_range", "\"size_bytes\" BETWEEN 0 AND 9223372036854775807");
                    table.CheckConstraint("ck_ingest_attempt_state_allowed", "\"state\" IN ('completed', 'failed', 'inProcess', 'interrupted', 'queued')");
                });

            migrationBuilder.CreateTable(
                name: "maintenance_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    close_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_cases", x => x.id);
                    table.CheckConstraint("ck_maintenance_case_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_maintenance_case_machine_id_not_empty", "\"machine_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_maintenance_case_state_allowed", "\"state\" IN ('closed', 'open', 'resolved')");
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
                name: "ingested_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    extension = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, collation: "case_insensitive"),
                    page_count = table.Column<int>(type: "integer", nullable: false),
                    chunk_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingested_documents", x => x.id);
                    table.CheckConstraint("ck_ingested_document_chunk_count_range", "\"chunk_count\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_ingested_document_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingested_document_extension_not_blank", "\"extension\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingested_document_page_count_range", "\"page_count\" BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_ingested_documents_ingest_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "ingest_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    role = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ingest_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    prompt_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, collation: "case_insensitive"),
                    prompt_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, collation: "case_insensitive"),
                    state = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    verdict = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    verdict_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "case_insensitive"),
                    error_code = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_runs", x => x.id);
                    table.CheckConstraint("ck_agent_run_attempt_number_range", "\"attempt_number\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_agent_run_error_code_allowed", "\"error_code\" IN ('caseChanged', 'noDecision', 'outputLimit', 'providerUnavailable', 'timeLimit', 'unexpectedError')");
                    table.CheckConstraint("ck_agent_run_input_tokens_range", "\"input_tokens\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_agent_run_output_tokens_range", "\"output_tokens\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_agent_run_role_allowed", "\"role\" IN ('chat', 'extractor', 'planner', 'screener', 'triage')");
                    table.CheckConstraint("ck_agent_run_skip_reason_allowed", "\"skip_reason\" IN ('caseNotOpen', 'nothingToReview')");
                    table.CheckConstraint("ck_agent_run_state_allowed", "\"state\" IN ('completed', 'failed', 'interrupted', 'queued', 'running', 'skipped')");
                    table.CheckConstraint("ck_agent_run_verdict_allowed", "\"verdict\" IN ('dismiss', 'escalate', 'safe', 'unsafe', 'watch')");
                    table.ForeignKey(
                        name: "FK_agent_runs_ingest_attempts_ingest_attempt_id",
                        column: x => x.ingest_attempt_id,
                        principalTable: "ingest_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agent_runs_maintenance_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "maintenance_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    severity = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    state = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    metric_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    status_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, collation: "case_insensitive"),
                    phase_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    value = table.Column<double>(type: "double precision", nullable: true),
                    limit = table.Column<double>(type: "double precision", nullable: true),
                    z = table.Column<double>(type: "double precision", nullable: true),
                    forecast_seconds = table.Column<double>(type: "double precision", nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coverage = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    work_order_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_signals", x => x.id);
                    table.CheckConstraint("ck_machine_signal_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_signal_coverage_allowed", "\"coverage\" IN ('dismissed', 'drafted', 'linked', 'new', 'watched')");
                    table.CheckConstraint("ck_machine_signal_machine_id_not_empty", "\"machine_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_signal_rule_allowed", "\"rule\" IN ('drift', 'silentMachine', 'status', 'threshold', 'trend', 'unknownMode')");
                    table.CheckConstraint("ck_machine_signal_sequence_range", "\"sequence\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_machine_signal_severity_allowed", "\"severity\" IN ('alarm', 'warning')");
                    table.CheckConstraint("ck_machine_signal_state_allowed", "\"state\" IN ('open', 'resolved')");
                    table.ForeignKey(
                        name: "FK_machine_signals_maintenance_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "maintenance_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    text = table.Column<string>(type: "text", nullable: false, collation: "default"),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_chunks_ingested_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "ingested_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    call_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    state = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    follow_up = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    follow_up_work_order_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_actions", x => x.id);
                    table.CheckConstraint("ck_agent_action_call_id_not_blank", "\"call_id\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_agent_action_follow_up_allowed", "\"follow_up\" IN ('applied', 'dismissed')");
                    table.CheckConstraint("ck_agent_action_kind_allowed", "\"kind\" IN ('closeCase', 'linkWorkOrder', 'proposeWorkOrder')");
                    table.CheckConstraint("ck_agent_action_state_allowed", "\"state\" IN ('buffered', 'committed', 'discarded')");
                    table.ForeignKey(
                        name: "FK_agent_actions_agent_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "agent_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_tool_calls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    call_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    arguments = table.Column<string>(type: "jsonb", nullable: false),
                    outcome = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    result = table.Column<string>(type: "jsonb", nullable: false),
                    duration_milliseconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_tool_calls", x => x.id);
                    table.CheckConstraint("ck_agent_tool_call_call_id_not_blank", "\"call_id\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_agent_tool_call_duration_milliseconds_range", "\"duration_milliseconds\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_agent_tool_call_outcome_allowed", "\"outcome\" IN ('failed', 'rejected', 'succeeded')");
                    table.CheckConstraint("ck_agent_tool_call_sequence_range", "\"sequence\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_agent_tool_call_tool_name_not_blank", "\"tool_name\" COLLATE \"default\" ~ '\\S'");
                    table.ForeignKey(
                        name: "FK_agent_tool_calls_agent_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "agent_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    kind = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    state = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    agent_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_code = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.CheckConstraint("ck_chat_message_chat_id_not_empty", "\"chat_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_message_error_code_allowed", "\"error_code\" IN ('caseChanged', 'noDecision', 'outputLimit', 'providerUnavailable', 'timeLimit', 'unexpectedError')");
                    table.CheckConstraint("ck_chat_message_kind_allowed", "\"kind\" IN ('answer', 'blockedNotice', 'refusal', 'unanswerable')");
                    table.CheckConstraint("ck_chat_message_role_allowed", "\"role\" IN ('assistant', 'user')");
                    table.CheckConstraint("ck_chat_message_sequence_range", "\"sequence\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_chat_message_state_allowed", "\"state\" IN ('cancelled', 'completed', 'failed', 'interrupted', 'streaming')");
                    table.CheckConstraint("ck_chat_message_text_user_text_length", "role <> 'user' OR (char_length(text) BETWEEN 1 AND 2000)");
                    table.ForeignKey(
                        name: "FK_chat_messages_agent_runs_agent_run_id",
                        column: x => x.agent_run_id,
                        principalTable: "agent_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_messages_chats_chat_id",
                        column: x => x.chat_id,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_message_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_committed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message_drafts", x => x.id);
                    table.CheckConstraint("ck_chat_message_draft_chat_message_id_not_empty", "\"chat_message_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_message_draft_draft_id_not_empty", "\"draft_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_message_draft_kind_allowed", "\"kind\" IN ('machine', 'metric', 'operatingPhase', 'status', 'workOrder')");
                    table.CheckConstraint("ck_chat_message_draft_title_not_blank", "\"title\" COLLATE \"default\" ~ '\\S'");
                    table.ForeignKey(
                        name: "FK_chat_message_drafts_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_message_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message_links", x => x.id);
                    table.CheckConstraint("ck_chat_message_link_chat_message_id_not_empty", "\"chat_message_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_message_link_entity_id_not_empty", "\"entity_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_chat_message_link_kind_allowed", "\"kind\" IN ('employee', 'file', 'machine', 'maintenanceCase', 'workOrder')");
                    table.CheckConstraint("ck_chat_message_link_label_not_blank", "\"label\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_chat_message_link_position_range", "\"position\" BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_chat_message_links_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_agent_actions_run_id_call_id",
                table: "agent_actions",
                columns: new[] { "run_id", "call_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_runs_case_id",
                table: "agent_runs",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_runs_ingest_attempt_id",
                table: "agent_runs",
                column: "ingest_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_runs_state_due_at",
                table: "agent_runs",
                columns: new[] { "state", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ux_agent_tool_calls_run_id_sequence",
                table: "agent_tool_calls",
                columns: new[] { "run_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_drafts_chat_message_id",
                table: "chat_message_drafts",
                column: "chat_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_links_chat_message_id",
                table: "chat_message_links",
                column: "chat_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_agent_run_id",
                table: "chat_messages",
                column: "agent_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_chat_messages_chat_id_sequence",
                table: "chat_messages",
                columns: new[] { "chat_id", "sequence" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chats_owner_user_id_company_id_last_activity_at",
                table: "chats",
                columns: new[] { "owner_user_id", "company_id", "last_activity_at" });

            migrationBuilder.CreateIndex(
                name: "ix_document_chunks_embedding_hnsw",
                table: "document_chunks",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);

            migrationBuilder.CreateIndex(
                name: "ux_document_chunks_document_id_ordinal",
                table: "document_chunks",
                columns: new[] { "document_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ingest_attempts_state_queued_at",
                table: "ingest_attempts",
                columns: new[] { "state", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ingest_attempts_active_file",
                table: "ingest_attempts",
                column: "file_id",
                unique: true,
                filter: "state IN ('queued', 'inProcess') AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ingest_attempts_completed_file",
                table: "ingest_attempts",
                column: "file_id",
                unique: true,
                filter: "state = 'completed' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ingested_documents_attempt_id",
                table: "ingested_documents",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingested_documents_company_id_machine_id",
                table: "ingested_documents",
                columns: new[] { "company_id", "machine_id" });

            migrationBuilder.CreateIndex(
                name: "ix_machine_signals_case_id",
                table: "machine_signals",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_signals_company_id_machine_id_state",
                table: "machine_signals",
                columns: new[] { "company_id", "machine_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ux_maintenance_cases_open_machine",
                table: "maintenance_cases",
                column: "machine_id",
                unique: true,
                filter: "state = 'open'");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_actions");

            migrationBuilder.DropTable(
                name: "agent_tool_calls");

            migrationBuilder.DropTable(
                name: "chat_message_drafts");

            migrationBuilder.DropTable(
                name: "chat_message_links");

            migrationBuilder.DropTable(
                name: "document_chunks");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "machine_signals");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "ingested_documents");

            migrationBuilder.DropTable(
                name: "agent_runs");

            migrationBuilder.DropTable(
                name: "chats");

            migrationBuilder.DropTable(
                name: "ingest_attempts");

            migrationBuilder.DropTable(
                name: "maintenance_cases");
        }
    }
}
