using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private const string CurrentRevisionForeignKeyName = "fk_work_orders_current_revision";

        private const string AddCurrentRevisionForeignKeySql =
            "ALTER TABLE work_orders ADD CONSTRAINT " + CurrentRevisionForeignKeyName
            + " FOREIGN KEY (id, current_revision) REFERENCES work_order_histories (work_order_id, revision)"
            + " DEFERRABLE INITIALLY DEFERRED;";

        private const string DropCurrentRevisionForeignKeySql =
            "ALTER TABLE work_orders DROP CONSTRAINT " + CurrentRevisionForeignKeyName + ";";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.CheckConstraint("ck_company_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_company_time_zone_id_length", "char_length(\"time_zone_id\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_company_time_zone_id_not_blank", "\"time_zone_id\" COLLATE \"default\" ~ '\\S'");
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
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("ck_role_code_allowed", "\"code\" IN ('supervisor', 'worker')");
                    table.CheckConstraint("ck_role_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, collation: "case_insensitive"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
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
                    table.PrimaryKey("PK_machines", x => x.id);
                    table.CheckConstraint("ck_machine_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_machine_gateway_id_length", "char_length(\"gateway_id\") BETWEEN 16 AND 16");
                    table.CheckConstraint("ck_machine_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_serial_number_not_blank", "\"serial_number\" COLLATE \"default\" ~ '\\S'");
                    table.ForeignKey(
                        name: "FK_machines_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false, collation: "case_insensitive"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
                    token_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_invites", x => x.id);
                    table.CheckConstraint("ck_employee_invite_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_employee_invite_email_not_blank", "\"email\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_invite_first_name_not_blank", "\"first_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_invite_last_name_not_blank", "\"last_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_invite_role_id_not_empty", "\"role_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_employee_invites_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_invites_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false, collation: "case_insensitive"),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    provisioning_retried_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.CheckConstraint("ck_employee_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_employee_email_not_blank", "\"email\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_first_name_not_blank", "\"first_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_last_name_not_blank", "\"last_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_role_id_not_empty", "\"role_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_employee_status_allowed", "\"status\" IN ('active', 'deactivated', 'provisioning', 'provisioningFailed')");
                    table.ForeignKey(
                        name: "FK_employees_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "case_insensitive"),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "case_insensitive"),
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
                    table.PrimaryKey("PK_machine_drafts", x => x.id);
                    table.CheckConstraint("ck_machine_draft_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_draft_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_machine_draft_name_length", "char_length(\"name\") BETWEEN 1 AND 200");
                    table.CheckConstraint("ck_machine_draft_name_not_blank", "\"name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_draft_serial_number_length", "char_length(\"serial_number\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_machine_draft_serial_number_not_blank", "\"serial_number\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_draft_state_allowed", "\"state\" IN ('applied', 'dismissed', 'pending')");
                    table.ForeignKey(
                        name: "FK_machine_drafts_machines_applied_machine_id",
                        column: x => x.applied_machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_machine_drafts_machines_target_machine_id",
                        column: x => x.target_machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "machine_state_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
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
                    table.PrimaryKey("PK_machine_state_histories", x => x.id);
                    table.CheckConstraint("ck_machine_state_history_machine_id_not_empty", "\"machine_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_state_history_state_allowed", "\"state\" IN ('inMaintenance', 'operational', 'outOfService')");
                    table.ForeignKey(
                        name: "FK_machine_state_histories_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    priority = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimate = table.Column<TimeSpan>(type: "interval", nullable: false),
                    current_revision = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.id);
                    table.CheckConstraint("ck_work_order_category_allowed", "\"category\" IN ('inspection', 'other', 'preventive', 'repair')");
                    table.CheckConstraint("ck_work_order_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_work_order_current_revision_range", "\"current_revision\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_work_order_description_length", "char_length(\"description\") BETWEEN 0 AND 20000");
                    table.CheckConstraint("ck_work_order_priority_allowed", "\"priority\" IN ('critical', 'high', 'low', 'medium')");
                    table.CheckConstraint("ck_work_order_title_length", "char_length(\"title\") BETWEEN 1 AND 200");
                    table.CheckConstraint("ck_work_order_title_not_blank", "\"title\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_work_orders_estimate_range", "estimate >= interval '5 minutes' AND estimate <= interval '720 minutes'");
                    table.ForeignKey(
                        name: "FK_work_orders_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_overrides", x => x.id);
                    table.CheckConstraint("ck_shift_override_employee_id_not_empty", "\"employee_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_shift_overrides_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_template_versions", x => x.id);
                    table.CheckConstraint("ck_shift_template_version_employee_id_not_empty", "\"employee_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_shift_template_versions_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metric_definition_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_machine_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
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
                    table.CheckConstraint("ck_metric_definition_draft_min_below_max", "min IS NULL OR max IS NULL OR min < max");
                    table.CheckConstraint("ck_metric_definition_draft_name_length", "char_length(\"name\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_metric_definition_draft_one_target", "num_nonnulls(target_machine_id, target_machine_draft_id) = 1");
                    table.CheckConstraint("ck_metric_definition_draft_source_location_length", "char_length(\"source_location\") BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_metric_definition_draft_state_allowed", "\"state\" IN ('applied', 'dismissed', 'pending')");
                    table.CheckConstraint("ck_metric_definition_draft_value_kind_allowed", "\"value_kind\" IN ('boolean', 'numeric', 'stateSet')");
                    table.ForeignKey(
                        name: "FK_metric_definition_drafts_machine_drafts_target_machine_draf~",
                        column: x => x.target_machine_draft_id,
                        principalTable: "machine_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metric_definition_drafts_machines_target_machine_id",
                        column: x => x.target_machine_id,
                        principalTable: "machines",
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
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_machine_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
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
                    table.CheckConstraint("ck_status_definition_draft_one_target", "num_nonnulls(target_machine_id, target_machine_draft_id) = 1");
                    table.CheckConstraint("ck_status_definition_draft_severity_allowed", "\"severity\" IN ('error', 'info', 'warning')");
                    table.CheckConstraint("ck_status_definition_draft_source_location_length", "char_length(\"source_location\") BETWEEN 0 AND 200");
                    table.CheckConstraint("ck_status_definition_draft_state_allowed", "\"state\" IN ('applied', 'dismissed', 'pending')");
                    table.ForeignKey(
                        name: "FK_status_definition_drafts_machine_drafts_target_machine_draf~",
                        column: x => x.target_machine_draft_id,
                        principalTable: "machine_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_status_definition_drafts_machines_target_machine_id",
                        column: x => x.target_machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_files", x => x.id);
                    table.CheckConstraint("ck_work_order_file_file_id_not_empty", "\"file_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_work_order_file_work_order_id_not_empty", "\"work_order_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_work_order_files_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_start_local = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    planned_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_histories", x => x.id);
                    table.UniqueConstraint("ak_work_order_histories_work_order_id_revision", x => new { x.work_order_id, x.revision });
                    table.CheckConstraint("ck_work_order_history_revision_range", "\"revision\" BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("ck_work_order_history_status_allowed", "\"status\" IN ('backlog', 'blocked', 'completed', 'inProgress', 'rejected', 'staged')");
                    table.CheckConstraint("ck_work_order_history_work_order_id_not_empty", "\"work_order_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_work_order_histories_employees_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_histories_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_override_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_override_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_override_blocks", x => x.id);
                    table.CheckConstraint("ck_shift_override_block_shift_override_id_not_empty", "\"shift_override_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_shift_override_blocks_end_after_start", "\"end\" > start");
                    table.ForeignKey(
                        name: "FK_shift_override_blocks_shift_overrides_shift_override_id",
                        column: x => x.shift_override_id,
                        principalTable: "shift_overrides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_template_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekday = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_template_blocks", x => x.id);
                    table.CheckConstraint("ck_shift_template_block_shift_template_version_id_not_empty", "\"shift_template_version_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_shift_template_block_weekday_allowed", "\"weekday\" IN ('friday', 'monday', 'saturday', 'sunday', 'thursday', 'tuesday', 'wednesday')");
                    table.CheckConstraint("ck_shift_template_blocks_end_after_start", "\"end\" > start");
                    table.ForeignKey(
                        name: "FK_shift_template_blocks_shift_template_versions_shift_templat~",
                        column: x => x.shift_template_version_id,
                        principalTable: "shift_template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_invites_company_id_email",
                table: "employee_invites",
                columns: new[] { "company_id", "email" },
                unique: true,
                filter: "accepted_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employee_invites_role_id",
                table: "employee_invites",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_company_id",
                table: "employees",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employees_identity_user_id",
                table: "employees",
                column: "identity_user_id",
                unique: true,
                filter: "identity_user_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employees_role_id",
                table: "employees",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_machine_drafts_applied_machine_id",
                table: "machine_drafts",
                column: "applied_machine_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_drafts_company_id_attempt_id",
                table: "machine_drafts",
                columns: new[] { "company_id", "attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_machine_drafts_target_machine_id_state",
                table: "machine_drafts",
                columns: new[] { "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_machine_state_histories_machine_id_started_at",
                table: "machine_state_histories",
                columns: new[] { "machine_id", "started_at" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_machines_company_id_serial_number",
                table: "machines",
                columns: new[] { "company_id", "serial_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_machines_gateway_id",
                table: "machines",
                column: "gateway_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_metric_definition_drafts_company_id_attempt_id",
                table: "metric_definition_drafts",
                columns: new[] { "company_id", "attempt_id" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_definition_drafts_target_machine_draft_id",
                table: "metric_definition_drafts",
                column: "target_machine_draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_metric_definition_drafts_target_machine_id_state",
                table: "metric_definition_drafts",
                columns: new[] { "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shift_override_blocks_shift_override_id",
                table: "shift_override_blocks",
                column: "shift_override_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_overrides_employee_id_date",
                table: "shift_overrides",
                columns: new[] { "employee_id", "date" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shift_template_blocks_shift_template_version_id",
                table: "shift_template_blocks",
                column: "shift_template_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_template_versions_employee_id_effective_from",
                table: "shift_template_versions",
                columns: new[] { "employee_id", "effective_from" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_status_definition_drafts_company_id_attempt_id",
                table: "status_definition_drafts",
                columns: new[] { "company_id", "attempt_id" });

            migrationBuilder.CreateIndex(
                name: "IX_status_definition_drafts_target_machine_draft_id",
                table: "status_definition_drafts",
                column: "target_machine_draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_status_definition_drafts_target_machine_id_state",
                table: "status_definition_drafts",
                columns: new[] { "target_machine_id", "state" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_files_work_order_id_file_id",
                table: "work_order_files",
                columns: new[] { "work_order_id", "file_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_histories_assignee_id_planned_start_utc",
                table: "work_order_histories",
                columns: new[] { "assignee_id", "planned_start_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_company_id",
                table: "work_orders",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_machine_id",
                table: "work_orders",
                column: "machine_id");

            migrationBuilder.Sql(AddCurrentRevisionForeignKeySql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropCurrentRevisionForeignKeySql);

            migrationBuilder.DropTable(
                name: "employee_invites");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "machine_state_histories");

            migrationBuilder.DropTable(
                name: "metric_definition_drafts");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "shift_override_blocks");

            migrationBuilder.DropTable(
                name: "shift_template_blocks");

            migrationBuilder.DropTable(
                name: "status_definition_drafts");

            migrationBuilder.DropTable(
                name: "work_order_files");

            migrationBuilder.DropTable(
                name: "work_order_histories");

            migrationBuilder.DropTable(
                name: "shift_overrides");

            migrationBuilder.DropTable(
                name: "shift_template_versions");

            migrationBuilder.DropTable(
                name: "machine_drafts");

            migrationBuilder.DropTable(
                name: "work_orders");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
