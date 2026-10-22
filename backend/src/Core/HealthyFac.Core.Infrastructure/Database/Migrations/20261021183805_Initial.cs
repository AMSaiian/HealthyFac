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
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.CheckConstraint("ck_company_name_not_blank", "\"name\" ~ '\\S'");
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    trace_parent = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    trace_parent = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("ck_role_code_not_blank", "\"code\" ~ '\\S'");
                    table.CheckConstraint("ck_role_name_not_blank", "\"name\" ~ '\\S'");
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.UniqueConstraint("AK_departments_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_department_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_department_name_not_blank", "\"name\" ~ '\\S'");
                    table.CheckConstraint("ck_department_time_zone_id_not_blank", "\"time_zone_id\" ~ '\\S'");
                    table.ForeignKey(
                        name: "FK_departments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    token_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_invites", x => x.id);
                    table.UniqueConstraint("AK_employee_invites_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_employee_invite_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_employee_invite_email_not_blank", "\"email\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_invite_first_name_not_blank", "\"first_name\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_invite_last_name_not_blank", "\"last_name\" ~ '\\S'");
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
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.UniqueConstraint("AK_employees_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_employee_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_employee_email_not_blank", "\"email\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_first_name_not_blank", "\"first_name\" ~ '\\S'");
                    table.CheckConstraint("ck_employee_last_name_not_blank", "\"last_name\" ~ '\\S'");
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
                name: "machines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machines", x => x.id);
                    table.CheckConstraint("ck_machine_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_department_id_not_empty", "\"department_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_name_not_blank", "\"name\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_serial_number_not_blank", "\"serial_number\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_state_allowed", "\"state\" IN ('inMaintenance', 'operational', 'outOfService')");
                    table.ForeignKey(
                        name: "FK_machines_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_machines_departments_department_id_company_id",
                        columns: x => new { x.department_id, x.company_id },
                        principalTable: "departments",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_invite_departments",
                columns: table => new
                {
                    employee_invite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_invite_departments", x => new { x.employee_invite_id, x.department_id });
                    table.ForeignKey(
                        name: "FK_employee_invite_departments_departments_department_id_compa~",
                        columns: x => new { x.department_id, x.company_id },
                        principalTable: "departments",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_invite_departments_employee_invites_employee_invit~",
                        columns: x => new { x.employee_invite_id, x.company_id },
                        principalTable: "employee_invites",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_departments",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_departments", x => new { x.employee_id, x.department_id });
                    table.ForeignKey(
                        name: "FK_employee_departments_departments_department_id_company_id",
                        columns: x => new { x.department_id, x.company_id },
                        principalTable: "departments",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_departments_employees_employee_id_company_id",
                        columns: x => new { x.employee_id, x.company_id },
                        principalTable: "employees",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_company_id_name",
                table: "departments",
                columns: new[] { "company_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_departments_department_id_company_id",
                table: "employee_departments",
                columns: new[] { "department_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_departments_employee_id_company_id",
                table: "employee_departments",
                columns: new[] { "employee_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_invite_departments_department_id_company_id",
                table: "employee_invite_departments",
                columns: new[] { "department_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_invite_departments_employee_invite_id_company_id",
                table: "employee_invite_departments",
                columns: new[] { "employee_invite_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_invites_company_id_email",
                table: "employee_invites",
                columns: new[] { "company_id", "email" },
                unique: true,
                filter: "accepted_at IS NULL");

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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_identity_user_id",
                table: "employees",
                column: "identity_user_id",
                unique: true,
                filter: "identity_user_id IS NOT NULL");

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
                name: "IX_machines_company_id",
                table: "machines",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_machines_department_id_company_id",
                table: "machines",
                columns: new[] { "department_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_departments");

            migrationBuilder.DropTable(
                name: "employee_invite_departments");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "employee_invites");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
