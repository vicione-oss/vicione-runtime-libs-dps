using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using ViciOne.Suite.DataPort;

namespace DataPortConnectionTester;

internal static class Program
{
    private static readonly Dictionary<string, Func<Task>> s_choices = new()
    {
        { "OpcUa.Client", OpcUaClientConnectionTestAsync },
        { "MySQL", MySqlConnectionTestAsync },
        { "PostgreSql", PostgreSqlConnectionTestAsync },
        { "Sql-Server", SqlServerConnectionTestAsync },
        { "Sqlite", SqliteConnectionTestAsync },
        { "Http.Client", HttpClientConnectionTestAsync },
        { "AWS Iot", AwsIotDataPortAsync },
        { "InfluxDB", InfluxDbConnectionTestAsync },
    };

    internal static async Task Main()
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("connection tests")
            .PageSize(11)
            .AddChoices(s_choices.Keys));

        try
        {
            await AnsiConsole.Status()
               .StartAsync("Trying to connect...", _ => s_choices[selection]());
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);

            Console.ReadLine();
        }
    }

    private static async Task OpcUaClientConnectionTestAsync()
    {
        OpcUaServerDataPortCommunication serverCommunication = new();
        _ = new OpcUaServerDataPortProperties(serverCommunication)
        {
            User = "",
            Server = "",
            Password = "",
            ApplicationCertificatesStorePath = "",
        };
        using var loggerFactory = new NullLoggerFactory();

        await using OpcUaServerDataPortIncoming incoming = new(serverCommunication, loggerFactory);
        await using OpcUaServerDataPortOutgoing outgoing = new(serverCommunication, loggerFactory);

        await incoming.ConnectAsync(CancellationToken.None);
        await outgoing.ConnectAsync(CancellationToken.None);

        OpcUaClientDataPortCommunication clientCommunication = new()
        {
            UserAuthenticationType = 1,
        };
        _ = new OpcUaClientDataPortProperties(clientCommunication)
        {
            Endpoint = "",
            Server = "",
            Password = "",
            User = "",
            ApplicationName = "",
            ApplicationCertificatesStorePath = "",
        };
        await OpcUaClientConnectionTester.TestConnectionAsync(clientCommunication);
    }

    private static async Task MySqlConnectionTestAsync()
    {
        MySqlDataPortCommunication communication = new()
        {
            Nodes = [],
        };
        _ = new MySqlDataPortProperties(communication)
        {
            Server = "",
            Port = 3306,
            UserId = "",
            Password = "",
            Database = "",
        };
        await MySqlConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task PostgreSqlConnectionTestAsync()
    {
        PostgreSqlDataPortCommunication communication = new()
        {
            Nodes = [],
        };
        _ = new PostgreSqlDataPortProperties(communication)
        {
            Host = "",
            Port = 5432,
            UserId = "",
            Password = "",
            Database = "",
        };
        await PostgreSqlConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task SqlServerConnectionTestAsync()
    {
        SqlServerDataPortCommunication communication = new()
        {
            Nodes = [],
        };
        _ = new SqlServerDataPortProperties(communication)
        {
            Host = "",
            Port = 1433,
            Database = "",
            UserID = "",
            Password = "",
        };
        await SqlServerConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task SqliteConnectionTestAsync()
    {
        SqliteDataPortCommunication communication = new()
        {
            Nodes = [],
        };
        _ = new SqliteDataPortProperties(communication)
        {
            DataSource = "",
        };
        await SqliteConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task HttpClientConnectionTestAsync()
    {
        HttpClientDataPortCommunication communication = new();
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "",
        };
        await HttpClientConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task AwsIotDataPortAsync()
    {
        AwsIotDataPortCommunication communication = new();
        _ = new AwsIotDataPortProperties(communication)
        {
            AccessKey = "",
            SecretAccessKey = ""
        };
        await AwsIotDataPortConnectionTester.TestConnectionAsync(communication);
    }

    private static async Task InfluxDbConnectionTestAsync()
    {
        InfluxDbDataPortCommunication communication = new();
        _ = new InfluxDbDataPortProperties(communication)
        {
            Organization = "",
            Url = "",
            Token = "",
        };
        await InfluxDbConnectionTester.TestConnectionAsync(communication);
    }
}
