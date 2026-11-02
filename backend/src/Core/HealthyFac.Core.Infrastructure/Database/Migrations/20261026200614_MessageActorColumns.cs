using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MessageActorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "error",
                table: "outbox_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "outbox_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "error",
                table: "inbox_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "inbox_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "inbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE outbox_messages SET actor_id = '5f0c4a51-7e1d-4c1b-9d0e-0a5c1d7e2b10' "
                + "WHERE actor_id = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.Sql(
                "UPDATE inbox_messages SET actor_id = '5f0c4a51-7e1d-4c1b-9d0e-0a5c1d7e2b10' "
                + "WHERE actor_id = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.Sql("ALTER TABLE outbox_messages ALTER COLUMN actor_id DROP DEFAULT;");

            migrationBuilder.Sql("ALTER TABLE inbox_messages ALTER COLUMN actor_id DROP DEFAULT;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_message_actor_id_not_empty",
                table: "outbox_messages",
                sql: "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inbox_message_actor_id_not_empty",
                table: "inbox_messages",
                sql: "\"actor_id\" <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inbox_message_id_not_empty",
                table: "inbox_messages",
                sql: "\"id\" <> '00000000-0000-0000-0000-000000000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inbox_message_id_not_empty",
                table: "inbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_message_actor_id_not_empty",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inbox_message_actor_id_not_empty",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "inbox_messages");

            migrationBuilder.AlterColumn<string>(
                name: "error",
                table: "outbox_messages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "error",
                table: "inbox_messages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
