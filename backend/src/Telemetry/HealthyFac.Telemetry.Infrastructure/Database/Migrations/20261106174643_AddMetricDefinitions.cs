using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    measurement_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metric_definitions");

            migrationBuilder.DropTable(
                name: "metric_schema_revisions");
        }
    }
}
