using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ManagedEngine.ExternalCommunication;
using ViciOne.Suite.DataPort;

namespace HttpServerTest;

internal static class Program
{
    public static async Task Main()
    {
        var parentID = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var jnodeId = Guid.NewGuid();

        var pathNode = new Node()
        {
            Id = pathId,
            DesignId = HttpServerNodeDesignId.PathPart,
            Name = "Value"
        };

        var parendNode = new Node()
        {
            Id = parentID,
            ParentId = pathId,
            DesignId = HttpServerNodeDesignId.HttpGetEndpoint,
            TransferredChannels = { nodeId.ToString() },
            Name = "get",
        };

        var jnode = new Node()
        {
            Id = jnodeId,
            ParentId = parentID,
            DesignId = HttpServerNodeDesignId.JsonStructure,
            AffectedChannels = { jnodeId.ToString() },
            Name = "Test",
        };

        var node = new Node()
        {
            Id = nodeId,
            ParentId = jnode.Id,
            DesignId = HttpServerNodeDesignId.Path,
            AffectedChannels = { nodeId.ToString() },
            Name = "Value",
            ValueType = typeof(double),
        };

        var communication = new HttpServerDataPortCommunication()
        {
            AuthenticationType = 0,
            BaseAddress = "https://localhost:5002",
            Nodes = [pathNode, parendNode, jnode, node],
            CertificateFile = @"",
            CertificateFilePassword = "",
        };

        var outgoing = new HttpServerDataPortOutgoing(communication, NullLoggerFactory.Instance.CreateLogger<HttpServerDataPortOutgoing>());
        var data = new ExternalValue()
        {
            Value = 0.1d,
            Channel = nodeId.ToString(),
        };

        await outgoing.SendAsync(0, [data], CancellationToken.None);

        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5002/Value");
        var result = await httpClient.SendAsync(request);
        var text = await result.Content.ReadAsStringAsync();

        Console.WriteLine(text);
        Console.ReadLine();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private static HttpClient CreateHttpClient()
        => new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
}
