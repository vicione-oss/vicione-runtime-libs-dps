using System;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class SqlServerDataPortCommunicationExtensions_ToConnectionString
{
    [Fact]
    public void Sets_mandatory_parameters()
        => CreateCommunication().ToConnectionString().Should()
        .Contain("Data Source=host;").And
        .Contain("Initial Catalog=db").And
        .Contain("Trust Server Certificate=True").And
        .NotContain("User ID").And
        .NotContain("Password");

    [Fact]
    public void Sets_Port_if_available()
        => CreateCommunication(c => c.Port = 1234).ToConnectionString().Should().Contain("Data Source=host,1234;");

    [Fact]
    public void Sets_UserID_if_available()
        => CreateCommunication(c => c.UserID = "user").ToConnectionString().Should().Contain("User ID=user");

    [Fact]
    public void Sets_Password_if_available()
        => CreateCommunication(c => c.Password = "pw").ToConnectionString().Should().Contain("Password=pw");

    private static SqlServerDataPortCommunication CreateCommunication(Action<SqlServerDataPortCommunication>? configure = default)
    {
        SqlServerDataPortCommunication communication = new() { Host = "host", Database = "db", };
        configure?.Invoke(communication);
        return communication;
    }
}
