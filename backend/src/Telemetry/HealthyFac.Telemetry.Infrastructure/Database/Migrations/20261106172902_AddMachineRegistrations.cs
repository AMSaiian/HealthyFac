using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "machine_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, collation: "case_insensitive"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_schema_revision = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_registrations", x => x.id);
                    table.CheckConstraint("ck_machine_registration_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_registration_department_id_not_empty", "\"department_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_machine_registration_gateway_id_length", "char_length(\"gateway_id\") BETWEEN 16 AND 16");
                });

            migrationBuilder.CreateIndex(
                name: "ix_machine_registrations_gateway_id",
                table: "machine_registrations",
                column: "gateway_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "machine_registrations");
        }
    }
}
