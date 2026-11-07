using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "status_definitions");
        }
    }
}
