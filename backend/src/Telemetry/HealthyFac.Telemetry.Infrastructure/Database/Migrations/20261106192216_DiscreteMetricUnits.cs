using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyFac.Telemetry.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class DiscreteMetricUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO quantity_kinds (id, name)
                SELECT gen_random_uuid(), 'Discrete'
                WHERE NOT EXISTS (SELECT 1 FROM quantity_kinds WHERE name = 'Discrete' AND deleted_at IS NULL);

                INSERT INTO measurement_units (id, quantity_kind_id, name, abbreviation)
                SELECT gen_random_uuid(), kind.id, discrete.name, ''
                FROM quantity_kinds kind
                CROSS JOIN (VALUES ('Boolean'), ('State')) AS discrete(name)
                WHERE kind.name = 'Discrete' AND kind.deleted_at IS NULL
                AND NOT EXISTS (
                    SELECT 1 FROM measurement_units unit
                    WHERE unit.quantity_kind_id = kind.id AND unit.name = discrete.name AND unit.deleted_at IS NULL);

                UPDATE metric_definitions SET measurement_unit_id = (
                    SELECT unit.id FROM measurement_units unit
                    JOIN quantity_kinds kind ON kind.id = unit.quantity_kind_id
                    WHERE kind.name = 'Discrete' AND kind.deleted_at IS NULL
                    AND unit.name = 'Boolean' AND unit.deleted_at IS NULL)
                WHERE value_kind = 'boolean' AND measurement_unit_id IS NULL;

                UPDATE metric_definitions SET measurement_unit_id = (
                    SELECT unit.id FROM measurement_units unit
                    JOIN quantity_kinds kind ON kind.id = unit.quantity_kind_id
                    WHERE kind.name = 'Discrete' AND kind.deleted_at IS NULL
                    AND unit.name = 'State' AND unit.deleted_at IS NULL)
                WHERE value_kind = 'stateSet' AND measurement_unit_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "measurement_unit_id",
                table: "metric_definitions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "measurement_unit_id",
                table: "metric_definitions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
