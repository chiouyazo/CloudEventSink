using System.Globalization;
using System.Text.Json.Nodes;
using CloudEventSink.Core.Query;
using CloudEventSink.Core.Query.Sql;
using Npgsql;

namespace CloudEventSink.Infrastructure.Query;

public sealed class QueryRunner : IQueryRunner
{
    private readonly string connectionString;

    public QueryRunner(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        this.connectionString = connectionString;
    }

    public async Task<QueryResultSet> ExecuteAsync(
        string commandText,
        IReadOnlyList<SqlParameterSpec> parameters,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        SqlStatementValidator.EnsureReadOnlySelect(commandText);

        await using NpgsqlConnection connection = new NpgsqlConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await SetSessionGuardsAsync(connection, transaction, cancellationToken)
            .ConfigureAwait(false);

        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        foreach (SqlParameterSpec parameter in parameters)
        {
            command.Parameters.Add(ToNpgsqlParameter(parameter));
        }

        QueryResultSet result = await ReadResultAsync(command, cancellationToken)
            .ConfigureAwait(false);
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static async Task SetSessionGuardsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlCommand setup = connection.CreateCommand();
        setup.Transaction = transaction;
        setup.CommandText =
            "SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = "
            + QueryLimits.StatementTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture);
        await setup.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<QueryResultSet> ReadResultAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlDataReader reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        List<QueryResultColumn> columns = new List<QueryResultColumn>(reader.FieldCount);
        for (int index = 0; index < reader.FieldCount; index++)
        {
            columns.Add(
                new QueryResultColumn
                {
                    Name = reader.GetName(index),
                    DataType = reader.GetDataTypeName(index),
                }
            );
        }

        List<IReadOnlyList<JsonNode?>> rows = new List<IReadOnlyList<JsonNode?>>();
        bool truncated = false;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count >= QueryLimits.MaxRows)
            {
                truncated = true;
                break;
            }

            JsonNode?[] row = new JsonNode?[reader.FieldCount];
            for (int index = 0; index < reader.FieldCount; index++)
            {
                row[index] = ConvertValue(reader, index);
            }

            rows.Add(row);
        }

        return new QueryResultSet
        {
            Columns = columns,
            Rows = rows,
            Truncated = truncated,
        };
    }

    private static JsonValue? ConvertValue(NpgsqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        object value = reader.GetValue(index);
        return value switch
        {
            bool booleanValue => JsonValue.Create(booleanValue),
            short shortValue => JsonValue.Create((long)shortValue),
            int intValue => JsonValue.Create((long)intValue),
            long longValue => JsonValue.Create(longValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            double doubleValue => JsonValue.Create(doubleValue),
            float floatValue => JsonValue.Create((double)floatValue),
            DateTime dateTimeValue => JsonValue.Create(
                dateTimeValue.ToString("o", CultureInfo.InvariantCulture)
            ),
            DateTimeOffset dateTimeOffsetValue => JsonValue.Create(
                dateTimeOffsetValue.ToString("o", CultureInfo.InvariantCulture)
            ),
            Guid guidValue => JsonValue.Create(guidValue.ToString()),
            string stringValue => JsonValue.Create(stringValue),
            _ => JsonValue.Create(value.ToString()),
        };
    }

    private static NpgsqlParameter ToNpgsqlParameter(SqlParameterSpec parameter)
    {
        return new NpgsqlParameter
        {
            ParameterName = parameter.Name,
            Value = parameter.Value ?? DBNull.Value,
        };
    }
}
