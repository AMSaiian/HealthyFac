using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Document.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class FileIngest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "machine_registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                collation: "case_insensitive");

            migrationBuilder.Sql("UPDATE machine_registrations SET name = id::text WHERE name = '';");

            migrationBuilder.AddColumn<DateTime>(
                name: "name_changed_at",
                table: "machine_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ingest_failed_count",
                table: "file_refs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ingest_state",
                table: "file_refs",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ingest_state_changed_at",
                table: "file_refs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "machine_changed_at",
                table: "file_refs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_registration_name_length",
                table: "machine_registrations",
                sql: "char_length(\"name\") BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "ck_machine_registration_name_not_blank",
                table: "machine_registrations",
                sql: "\"name\" COLLATE \"default\" ~ '\\S'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_file_ref_ingest_failed_count_range",
                table: "file_refs",
                sql: "\"ingest_failed_count\" BETWEEN 0 AND 2147483647");

            migrationBuilder.AddCheckConstraint(
                name: "ck_file_ref_ingest_state_allowed",
                table: "file_refs",
                sql: "\"ingest_state\" IN ('completed', 'failed', 'inProcess', 'interrupted', 'queued')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_registration_name_length",
                table: "machine_registrations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_machine_registration_name_not_blank",
                table: "machine_registrations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_file_ref_ingest_failed_count_range",
                table: "file_refs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_file_ref_ingest_state_allowed",
                table: "file_refs");

            migrationBuilder.DropColumn(
                name: "name",
                table: "machine_registrations");

            migrationBuilder.DropColumn(
                name: "name_changed_at",
                table: "machine_registrations");

            migrationBuilder.DropColumn(
                name: "ingest_failed_count",
                table: "file_refs");

            migrationBuilder.DropColumn(
                name: "ingest_state",
                table: "file_refs");

            migrationBuilder.DropColumn(
                name: "ingest_state_changed_at",
                table: "file_refs");

            migrationBuilder.DropColumn(
                name: "machine_changed_at",
                table: "file_refs");
        }
    }
}
