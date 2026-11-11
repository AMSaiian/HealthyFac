using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class FileIngest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "machines",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "machine_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
                name: "metric_definition_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metric_definition_drafts");

            migrationBuilder.DropTable(
                name: "status_definition_drafts");

            migrationBuilder.DropTable(
                name: "machine_drafts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "machines");
        }
    }
}
