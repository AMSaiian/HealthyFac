using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    raw_payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    errors = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_received_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_received_messages_machine_registrations_machine_registratio~",
                        column: x => x.machine_registration_id,
                        principalTable: "machine_registrations",
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

            migrationBuilder.CreateIndex(
                name: "ix_received_messages_machine_registration_id_sampled_at",
                table: "received_messages",
                columns: new[] { "machine_registration_id", "sampled_at" },
                unique: true,
                filter: "machine_registration_id IS NOT NULL AND sampled_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discrete_readings");

            migrationBuilder.DropTable(
                name: "numeric_readings");

            migrationBuilder.DropTable(
                name: "received_messages");

            migrationBuilder.DropTable(
                name: "status_readings");
        }
    }
}
