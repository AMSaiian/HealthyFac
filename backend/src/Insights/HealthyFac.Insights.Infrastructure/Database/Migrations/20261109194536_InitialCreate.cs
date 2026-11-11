using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace HealthyFac.Insights.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

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
                name: "ingest_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    extension = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, collation: "case_insensitive"),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    state = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    error_code = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    interrupt_reason = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    warning = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "case_insensitive"),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trace_parent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true, collation: "case_insensitive"),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    chunk_count = table.Column<int>(type: "integer", nullable: true),
                    proposed_draft_count = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_ingest_attempts", x => x.id);
                    table.CheckConstraint("ck_ingest_attempt_chunk_count_not_negative", "chunk_count IS NULL OR chunk_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingest_attempt_error_code_allowed", "\"error_code\" IN ('conversionFailed', 'conversionTimeout', 'fileNotFound', 'invalidExtraction', 'noText', 'providerUnavailable', 'tooManyPages')");
                    table.CheckConstraint("ck_ingest_attempt_error_code_only_when_failed", "error_code IS NULL OR state = 'failed'");
                    table.CheckConstraint("ck_ingest_attempt_extension_not_blank", "\"extension\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingest_attempt_file_id_not_empty", "\"file_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingest_attempt_file_name_not_blank", "\"file_name\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingest_attempt_finished_at_only_when_final", "finished_at IS NULL OR state NOT IN ('queued', 'inProcess')");
                    table.CheckConstraint("ck_ingest_attempt_interrupt_reason_allowed", "\"interrupt_reason\" IN ('cancelled', 'fileDeleted', 'leaseExpired', 'serviceStopped')");
                    table.CheckConstraint("ck_ingest_attempt_interrupt_reason_only_when_interrupted", "interrupt_reason IS NULL OR state = 'interrupted'");
                    table.CheckConstraint("ck_ingest_attempt_page_count_not_negative", "page_count IS NULL OR page_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_proposed_draft_count_not_negative", "proposed_draft_count IS NULL OR proposed_draft_count >= 0");
                    table.CheckConstraint("ck_ingest_attempt_size_bytes_range", "\"size_bytes\" BETWEEN 0 AND 9223372036854775807");
                    table.CheckConstraint("ck_ingest_attempt_state_allowed", "\"state\" IN ('completed', 'failed', 'inProcess', 'interrupted', 'queued')");
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
                name: "ingested_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    extension = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, collation: "case_insensitive"),
                    page_count = table.Column<int>(type: "integer", nullable: false),
                    chunk_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingested_documents", x => x.id);
                    table.CheckConstraint("ck_ingested_document_chunk_count_range", "\"chunk_count\" BETWEEN 0 AND 2147483647");
                    table.CheckConstraint("ck_ingested_document_company_id_not_empty", "\"company_id\" <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_ingested_document_extension_not_blank", "\"extension\" COLLATE \"default\" ~ '\\S'");
                    table.CheckConstraint("ck_ingested_document_page_count_range", "\"page_count\" BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_ingested_documents_ingest_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "ingest_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    text = table.Column<string>(type: "text", nullable: false, collation: "default"),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_chunks_ingested_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "ingested_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_chunks_embedding_hnsw",
                table: "document_chunks",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);

            migrationBuilder.CreateIndex(
                name: "ux_document_chunks_document_id_ordinal",
                table: "document_chunks",
                columns: new[] { "document_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_next_attempt_on",
                table: "inbox_messages",
                column: "next_attempt_on",
                filter: "processed_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ingest_attempts_state_queued_at",
                table: "ingest_attempts",
                columns: new[] { "state", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ingest_attempts_active_file",
                table: "ingest_attempts",
                column: "file_id",
                unique: true,
                filter: "state IN ('queued', 'inProcess') AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ingest_attempts_completed_file",
                table: "ingest_attempts",
                column: "file_id",
                unique: true,
                filter: "state = 'completed' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ingested_documents_attempt_id",
                table: "ingested_documents",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingested_documents_company_id_machine_id",
                table: "ingested_documents",
                columns: new[] { "company_id", "machine_id" });

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
                name: "document_chunks");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "ingested_documents");

            migrationBuilder.DropTable(
                name: "ingest_attempts");
        }
    }
}
