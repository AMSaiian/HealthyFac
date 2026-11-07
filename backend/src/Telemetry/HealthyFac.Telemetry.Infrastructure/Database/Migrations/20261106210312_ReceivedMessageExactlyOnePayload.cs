using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReceivedMessageExactlyOnePayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "raw_payload",
                table: "received_messages",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.Sql("UPDATE received_messages SET raw_payload = NULL WHERE payload IS NOT NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_received_message_exactly_one_payload_form",
                table: "received_messages",
                sql: "(payload IS NULL) <> (raw_payload IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_received_message_exactly_one_payload_form",
                table: "received_messages");

            migrationBuilder.AlterColumn<byte[]>(
                name: "raw_payload",
                table: "received_messages",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
