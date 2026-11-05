using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveMachineStateToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_machine_state_histories_machine_id_started_at",
                table: "machine_state_histories",
                columns: new[] { "machine_id", "started_at" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.Sql(
                """
                INSERT INTO machine_state_histories
                    (id, machine_id, state, started_at, created_at, created_by, deleted_at, deleted_by)
                SELECT uuidv7(), id, state, created_at, created_at, created_by, deleted_at, deleted_by
                FROM machines;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_state_allowed",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "state",
                table: "machines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "machines",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "operational");

            migrationBuilder.Sql(
                """
                UPDATE machines
                SET state = latest.state
                FROM (
                    SELECT DISTINCT ON (machine_id) machine_id, state
                    FROM machine_state_histories
                    WHERE deleted_at IS NULL
                    ORDER BY machine_id, started_at DESC
                ) AS latest
                WHERE machines.id = latest.machine_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "machines",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13,
                oldDefaultValue: "operational");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_state_allowed",
                table: "machines",
                sql: "\"state\" IN ('inMaintenance', 'operational', 'outOfService')");

            migrationBuilder.DropTable(
                name: "machine_state_histories");
        }
    }
}
