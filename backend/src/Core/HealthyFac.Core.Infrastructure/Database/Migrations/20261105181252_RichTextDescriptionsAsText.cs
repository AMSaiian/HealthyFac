using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RichTextDescriptionsAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_description_length",
                table: "machines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_error_code_description_length",
                table: "machine_error_codes");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "machines",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "machine_error_codes",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_description_length",
                table: "machines",
                sql: "char_length(\"description\") BETWEEN 0 AND 20000");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_error_code_description_length",
                table: "machine_error_codes",
                sql: "char_length(\"description\") BETWEEN 0 AND 20000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_description_length",
                table: "machines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_error_code_description_length",
                table: "machine_error_codes");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "machines",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "machine_error_codes",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_description_length",
                table: "machines",
                sql: "char_length(\"description\") BETWEEN 0 AND 5000");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_error_code_description_length",
                table: "machine_error_codes",
                sql: "char_length(\"description\") BETWEEN 0 AND 5000");
        }
    }
}
