using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
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
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_invites");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "machine_state_histories");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
