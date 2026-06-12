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
        { "Http.Client", HttpClientConnectionTestAsync },
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

    private static async Task HttpClientConnectionTestAsync()
    {
        HttpClientDataPortCommunication communication = new();
        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = "",
        };
        await HttpClientConnectionTester.TestConnectionAsync(communication);
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
