using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace HealthyFac.Seeder.TelemetryHistory.Persistence;

/// <summary>
/// Writes the received messages and the readings of the history with the Npgsql binary import, because the history
/// holds millions of rows. The table, the columns, the values, and the data types come from the EF model of the
/// Telemetry database, so a change of the model reaches the writer without an edit. The import does not use the
/// change tracker and sets no audit field: the tables of these 4 entities have none.
/// </summary>
/// <remarks>
/// One call is one transaction on the connection of the context. A cancelled call rolls back, so it leaves no row.
/// </remarks>
public sealed class ReadingRowWriter
{
    private const char QuoteCharacter = '"';

    /// <summary>Writes the rows of a batch of messages.</summary>
    /// <param name="dbContext">The context whose connection and model the import uses.</param>
    /// <param name="batch">The messages with their readings.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the transaction is committed.</returns>
    public async Task WriteAsync(
        TelemetryDbContext dbContext,
        IReadOnlyList<HistoryRows> batch,
        CancellationToken cancellationToken
    )
    {
        if (batch.Count == 0)
        {
            return;
        }

        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var idOverrides = batch.Where(rows => rows.MessageId is not null)
            .ToDictionary(
                rows => (object)rows.Received, rows => rows.MessageId!.Value, ReferenceEqualityComparer.Instance);

        await ImportAsync(
            connection, dbContext, batch.Select(rows => (object)rows.Received), idOverrides, cancellationToken);
        await ImportAsync(
            connection,
            dbContext,
            batch.SelectMany(rows => rows.Numeric).Cast<object>(),
            idOverrides: null,
            cancellationToken);
        await ImportAsync(
            connection,
            dbContext,
            batch.SelectMany(rows => rows.Discrete).Cast<object>(),
            idOverrides: null,
            cancellationToken);
        await ImportAsync(
            connection,
            dbContext,
            batch.SelectMany(rows => rows.Statuses).Cast<object>(),
            idOverrides: null,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ImportAsync(
        NpgsqlConnection connection,
        DbContext dbContext,
        IEnumerable<object> entities,
        IReadOnlyDictionary<object, Guid>? idOverrides,
        CancellationToken cancellationToken
    )
    {
        using var enumerator = entities.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        var entityType = dbContext.Model.FindEntityType(enumerator.Current.GetType())!;
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var properties = entityType.GetProperties().ToList();
        var idProperty = entityType.FindProperty(nameof(ReceivedMessage.Id));
        var columns = properties.Select(property => Quote(property.GetColumnName(table)!));
        var target = table.Schema is null ? Quote(table.Name) : $"{Quote(table.Schema)}.{Quote(table.Name)}";
        var typeNames = properties.Select(DataTypeNameOf).ToList();
        var command = $"COPY {target} ({string.Join(separator: ", ", columns)}) FROM STDIN (FORMAT BINARY)";

        await using var importer = await connection.BeginBinaryImportAsync(command, cancellationToken);
        do
        {
            var entity = enumerator.Current;
            await importer.StartRowAsync(cancellationToken);
            for (var index = 0; index < properties.Count; index++)
            {
                var property = properties[index];
                var value = idOverrides is not null
                    && property == idProperty
                    && idOverrides.TryGetValue(entity, out var overrideId)
                        ? overrideId
                        : property.GetGetter().GetClrValue(entity);
                var provided = property.GetValueConverter() is { } converter && value is not null
                    ? converter.ConvertToProvider(value)
                    : value;

                await WriteValueAsync(importer, provided, typeNames[index], cancellationToken);
            }
        }
        while (enumerator.MoveNext());

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task WriteValueAsync(
        NpgsqlBinaryImporter importer,
        object? value,
        string dataTypeName,
        CancellationToken cancellationToken
    )
    {
        switch (value)
        {
            case null:
                await importer.WriteNullAsync(cancellationToken);
                break;
            case Guid guid:
                await importer.WriteAsync(guid, dataTypeName, cancellationToken);
                break;
            case DateTime dateTime:
                await importer.WriteAsync(dateTime, dataTypeName, cancellationToken);
                break;
            case double number:
                await importer.WriteAsync(number, dataTypeName, cancellationToken);
                break;
            case bool flag:
                await importer.WriteAsync(flag, dataTypeName, cancellationToken);
                break;
            case int integer:
                await importer.WriteAsync(integer, dataTypeName, cancellationToken);
                break;
            case string text:
                await importer.WriteAsync(text, dataTypeName, cancellationToken);
                break;
            case byte[] bytes:
                await importer.WriteAsync(bytes, dataTypeName, cancellationToken);
                break;
            default:
                throw new InvalidOperationException(
                    TelemetryHistoryErrors.ColumnTypeNotSupported(value.GetType().Name));
        }
    }

    private static string DataTypeNameOf(IProperty property)
    {
        var columnType = property.GetColumnType();
        var length = columnType.IndexOf(value: '(', StringComparison.Ordinal);

        return length < 0 ? columnType : columnType[..length].Trim();
    }

    private static string Quote(string name)
    {
        var escaped = name.Replace(
            QuoteCharacter.ToString(), $"{QuoteCharacter}{QuoteCharacter}", StringComparison.Ordinal);

        return $"{QuoteCharacter}{escaped}{QuoteCharacter}";
    }
}
