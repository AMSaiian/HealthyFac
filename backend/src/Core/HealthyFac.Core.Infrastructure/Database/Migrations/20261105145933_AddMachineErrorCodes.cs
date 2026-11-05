using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineErrorCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "machine_error_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_error_codes", x => x.id);
                    table.CheckConstraint("ck_machine_error_code_code_not_blank", "\"code\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_machine_error_code_description_length", "char_length(\"description\") BETWEEN 0 AND 5000");
                    table.CheckConstraint("ck_machine_error_code_machine_id_not_empty", "\"machine_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_machine_error_codes_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_machine_error_codes_machine_id_code",
                table: "machine_error_codes",
                columns: new[] { "machine_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "machine_error_codes");
        }
    }
}
