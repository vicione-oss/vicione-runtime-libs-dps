using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ViciOne.DataPort.Communication;

namespace ViciOne.Suite.DataPort;

internal sealed class SqlServerRelationalDatabase(SqlServerDataPortCommunication communication, ILogger logger) : RelationalDatabase(communication, logger)
{
    internal const string SingleDatabaseInstanceNodeId = "SqlServer-SingleDatabaseInstance";

    private readonly SqlServerDataPortCommunication _communication = communication;
    private readonly ILogger _logger = logger;
    private SqlConnection? _connection;
    private readonly Dictionary<TableInfo, DataTable> _tables = [];

    private bool IsSingleDatabaseMode => _communication.DesignId == SingleDatabaseInstanceNodeId;

    protected override async Task OpenConnectionAsync(IReadOnlyCollection<ColumnInfo> columnInfos, CancellationToken cancellationToken)
    {
        if (IsSingleDatabaseMode)
        {
            var databases = columnInfos.Select(c => c.TableInfo.Database).Distinct().ToList();

            if (databases.Count > 1)
            {
                throw new InvalidOperationException(
                    $"More than one database detected but SingleDatabaseInstance node is active — only one database can be managed. Detected databases: {string.Join(", ", databases)}.");
            }

            if (databases.Count == 1)
                _communication.Database = databases[0];
        }

        FillTablesCache();
        await ConnectAsync().ConfigureAwait(false);

        void FillTablesCache()
        {
            foreach (var tableColumns in columnInfos.GroupBy(c => c.TableInfo))
                _tables.Add(tableColumns.Key, CreateDataTableTemplate(tableColumns.Key, tableColumns));
        }

        async Task ConnectAsync()
        {
            _connection ??= new(_communication.ToConnectionString());
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static DataTable CreateDataTableTemplate(TableInfo tableInfo, IEnumerable<ColumnInfo> columnInfos)
    {
        var tableName = SqlServerCommands.ComposeTableNameTwoPart(tableInfo);
        DataTable dataTable = new(tableName);

        foreach (var columnInfo in columnInfos)
        {
            if (columnInfo.Type != null)
                dataTable.Columns.Add(columnInfo.Name, columnInfo.Type);
            else
                dataTable.Columns.Add(columnInfo.Name);
        }

        return dataTable;
    }

    protected override async Task CloseConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            SqlConnection.ClearPool(_connection);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }

        foreach (var table in _tables.Values)
            table.Dispose();
        _tables.Clear();
    }

    [SuppressMessage("Security", "CA2100:SQL-Abfragen auf Sicherheitsrisiken überprüfen")]
    protected override async Task CreateMissingStructureAsync(IReadOnlyCollection<Database> missingTables, IReadOnlyCollection<Database> missingDatabases,
        IReadOnlyCollection<Database> missingSchemas, CancellationToken cancellationToken)
    {
        await AdoDotNetFunctions.EnsureConnectionAsync(_connection, cancellationToken).ConfigureAwait(false);
        using var command = _connection.CreateCommand();

        if (IsSingleDatabaseMode)
        {
            if (missingDatabases.Count != 0)
                _logger.LogWarning("SingleDatabaseInstance node is active – automatic database creation is not supported. Ensure the database already exists before connecting.");

            foreach (var database in missingSchemas)
            {
                foreach (var schema in database.Schemas)
                {
                    command.CommandText = SqlServerCommands.ComposeCreateSchemaCommandText(schema);
                    await AdoDotNetFunctions.ExecuteTransactionAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }

            if (missingTables.Count != 0)
            {
                command.CommandText = SqlServerCommands.ComposeCreateTablesCommandText(missingTables, singleDatabase: true);
                await AdoDotNetFunctions.ExecuteTransactionAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            if (missingDatabases.Count != 0)
            {
                command.CommandText = SqlServerCommands.ComposeCreateDatabasesCommandText(missingDatabases);
                await AdoDotNetFunctions.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
            }

            foreach (var database in missingSchemas)
            {
                command.CommandText = SqlServerCommands.ComposeUseDatabaseCommandText(database);
                await AdoDotNetFunctions.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

                foreach (var schema in database.Schemas)
                {
                    command.CommandText = SqlServerCommands.ComposeCreateSchemaCommandText(schema);
                    await AdoDotNetFunctions.ExecuteTransactionAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }

            if (missingTables.Count != 0)
            {
                command.CommandText = SqlServerCommands.ComposeCreateTablesCommandText(missingTables);
                await AdoDotNetFunctions.ExecuteTransactionAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [SuppressMessage("Security", "CA2100:SQL-Abfragen auf Sicherheitsrisiken überprüfen")]
    protected override async Task<IReadOnlyCollection<Database>> GetExistingStructureAsync(CancellationToken cancellationToken)
    {
        await AdoDotNetFunctions.EnsureConnectionAsync(_connection, cancellationToken).ConfigureAwait(false);
        using var command = _connection.CreateCommand();

        if (IsSingleDatabaseMode)
        {
            command.CommandText = SqlServerCommands.SelectAllSchemasWithInformation;
            var result = await AdoDotNetFunctions.ExecuteReaderAsync(command, SqlServerCommands.ReadSchemaInfo, cancellationToken).ConfigureAwait(false);
            return [new Database(_communication.Database, result.ToSchemas())];
        }

        command.CommandText = SqlServerCommands.SelectAllDatabases;
        var databaseNames = await AdoDotNetFunctions.ExecuteReaderAsync(command, SqlServerCommands.ReadDatabase, cancellationToken).ConfigureAwait(false);
        List<Database> databases = [];

        foreach (var database in databaseNames)
        {
            command.CommandText = SqlServerCommands.ComposeSelectStructureCommandText(database);
            var result = await AdoDotNetFunctions.ExecuteReaderAsync(command, SqlServerCommands.ReadSchemaInfo, cancellationToken).ConfigureAwait(false);

            databases.Add(new Database(database, result.ToSchemas()));
        }

        return databases;
    }

    protected override IReadOnlyCollection<Database> GetExpectedStructure(IReadOnlyCollection<INode> nodes)
        => nodes.Unify();

    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose();
        foreach (var table in _tables.Values)
            table.Dispose();
        _tables.Clear();
        base.Dispose(disposing);
    }

    protected override async ValueTask<bool> PublishAsync(IEnumerable<RowData> data, CancellationToken cancellationToken)
    {
        try
        {
            await AdoDotNetFunctions.EnsureConnectionAsync(_connection, cancellationToken).ConfigureAwait(false);
            await PublishToSqlServerAsync(_connection, data, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.PublishFailed(ex);
            return false;
        }

        return true;
    }

    private async Task PublishToSqlServerAsync(SqlConnection connection, IEnumerable<RowData> data, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction);

        foreach (var dbGroup in data.GroupBy(d => d.TableInfo.Database))
        {
            await connection.ChangeDatabaseAsync(dbGroup.Key, cancellationToken).ConfigureAwait(false);

            foreach (var rowData in dbGroup.GroupBy(d => d.TableInfo))
            {
                var dataTable = _tables[rowData.Key];
                var rows = CreateDataRowsFromTemplate(dataTable, rowData).ToArray();
                bulkCopy.DestinationTableName = dataTable.TableName;
                await bulkCopy.WriteToServerAsync(rows, cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static IEnumerable<DataRow> CreateDataRowsFromTemplate(DataTable dataTable, IEnumerable<RowData> data)
    {
        foreach (var rowData in data)
        {
            var row = dataTable.NewRow();

            foreach (var column in rowData.Columns)
                row[column.Key.Name] = column.Value;

            yield return row;
        }
    }

    protected override Dictionary<Guid, ColumnInfo> CreateColumnInfos(IReadOnlyCollection<INode> nodes)
        => SqlServerExtensions.CreateColumnInfos(nodes);
}
