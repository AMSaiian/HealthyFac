using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Document.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, collation: "case_insensitive"),
                    trace_parent = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.id);
                    table.CheckConstraint("ck_inbox_message_actor_id_not_empty", "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_inbox_message_id_not_empty", "\"id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "machine_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_registrations", x => x.id);
                    table.CheckConstraint("ck_machine_registration_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_attempt_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, collation: "case_insensitive"),
                    trace_parent = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_message_actor_id_not_empty", "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateTable(
                name: "file_refs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    extension = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, collation: "case_insensitive"),
                    content_type = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false, collation: "case_insensitive"),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_refs", x => x.id);
                    table.CheckConstraint("ck_file_ref_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_file_ref_content_type_not_blank", "\"content_type\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_file_ref_description_length", "char_length(\"description\") BETWEEN 0 AND 500");
                    table.CheckConstraint("ck_file_ref_extension_allowed", "\"extension\" IN ('csv', 'docx', 'gif', 'jpg', 'md', 'pdf', 'png', 'pptx', 'txt', 'webp', 'xlsx')");
                    table.CheckConstraint("ck_file_ref_file_name_length", "char_length(\"file_name\") BETWEEN 1 AND 255");
                    table.CheckConstraint("ck_file_ref_file_name_not_blank", "\"file_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_file_ref_machine_id_embedded_without_machine", "purpose <> 'embedded' OR machine_id IS NULL");
                    table.CheckConstraint("ck_file_ref_purpose_allowed", "\"purpose\" IN ('attachment', 'embedded')");
                    table.CheckConstraint("ck_file_ref_sha256_hash_length", "octet_length(sha256) = 32");
                    table.CheckConstraint("ck_file_ref_size_bytes_range", "\"size_bytes\" BETWEEN 1 AND 9223372036854775807");
                    table.ForeignKey(
                        name: "FK_file_refs_machine_registrations_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machine_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_refs_company_id_machine_id_purpose",
                table: "file_refs",
                columns: new[] { "company_id", "machine_id", "purpose" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_file_refs_machine_id",
                table: "file_refs",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_next_attempt_on",
                table: "outbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_refs");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "machine_registrations");
        }
    }
}
