using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RoleCodeEnumeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_role_code_not_blank",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "roles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddCheckConstraint(
                name: "ck_role_code_allowed",
                table: "roles",
                sql: "\"code\" IN ('supervisor', 'worker')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_role_code_allowed",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddCheckConstraint(
                name: "ck_role_code_not_blank",
                table: "roles",
                sql: "\"code\" ~ '\\S'");
        }
    }
}
