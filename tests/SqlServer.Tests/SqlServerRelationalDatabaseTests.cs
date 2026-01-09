using System;
using System.Collections.Generic;
using System.Data;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class SqlServerRelationalDatabase_CreateDataRowsFromTemplate
{
    [Fact]
    public void Acceptance()
    {
        using DataTable dataTable = new();
        dataTable.Columns.Add("id");
        dataTable.Columns.Add("name");
        TableInfo table = new("MyDB", "dbo", "users");
        ColumnInfo idColumn = new(table, "id", false, false, typeof(Guid));
        ColumnInfo nameColumn = new(table, "name", false, false, typeof(string));
        List<RowData> rowData =
        [
            new(table, new() { { idColumn, 23 }, { nameColumn, "Test1" }, }),
            new(table, new() { { idColumn, 24 }, { nameColumn, "Test2" }, }),
        ];

        var dataRows = SqlServerRelationalDatabase.CreateDataRowsFromTemplate(dataTable, rowData);

        dataTable.Rows.Count.Should().Be(0);
        dataRows.Should().HaveCount(2).And.SatisfyRespectively(
            r =>
            {
                r["id"].Should().Be("23");
                r["name"].Should().Be("Test1");
            },
            r =>
            {
                r["id"].Should().Be("24");
                r["name"].Should().Be("Test2");
            });
    }
}

