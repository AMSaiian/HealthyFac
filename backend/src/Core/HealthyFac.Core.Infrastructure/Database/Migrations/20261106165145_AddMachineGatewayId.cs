using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineGatewayId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gateway_id",
                table: "machines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                collation: "case_insensitive");

            migrationBuilder.Sql(
                "UPDATE machines SET gateway_id = translate(substr(md5(random()::text || id::text), 1, 16), '0189', 'wxyz');");

            migrationBuilder.CreateIndex(
                name: "ix_machines_gateway_id",
                table: "machines",
                column: "gateway_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_gateway_id_length",
                table: "machines",
                sql: "char_length(\"gateway_id\") BETWEEN 16 AND 16");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_machines_gateway_id",
                table: "machines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_gateway_id_length",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "gateway_id",
                table: "machines");
        }
    }
}
