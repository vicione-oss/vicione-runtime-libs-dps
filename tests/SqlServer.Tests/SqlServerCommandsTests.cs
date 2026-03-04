using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class SqlServerCommands_ComposeCreateTableCommandText
{
    [Fact]
    public void Acceptance()
    {
        Table table = new("users",
        [
            new("id", "int", true, true, true),
            new("name", "text", false, false, false),
            new("age", "int", false, true, false),
        ]);
        Schema schema = new("dbo", [table,]);
        Database database = new("MyDB", [schema,]);

        SqlServerCommands.ComposeCreateTableCommandText(database, schema, table).Should().Be("""
        CREATE TABLE "MyDB"."dbo"."users" (
        "id" int IDENTITY(1,1) PRIMARY KEY NOT NULL,
        "name" text,
        "age" int NOT NULL);
        """.ReplaceLineEndings());
    }
}
