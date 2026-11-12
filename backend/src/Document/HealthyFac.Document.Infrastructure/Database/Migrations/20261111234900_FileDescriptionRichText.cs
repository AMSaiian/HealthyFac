using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Document.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class FileDescriptionRichText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_file_ref_description_length",
                table: "file_refs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_file_ref_description_length",
                table: "file_refs",
                sql: "char_length(\"description\") BETWEEN 0 AND 20000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_file_ref_description_length",
                table: "file_refs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_file_ref_description_length",
                table: "file_refs",
                sql: "char_length(\"description\") BETWEEN 0 AND 500");
        }
    }
}
