using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Suite.DataPort;

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

        var parenNode = new Node()
        {
            Id = parentID,
            ParentId = pathId,
            DesignId = HttpServerNodeDesignId.HttpPostEndpoint,
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
            Nodes = [pathNode, parenNode, jnode, node],
            CertificateFile = @"",
            CertificateFilePassword = "",
        };

        var incoming = new HttpServerDataPortIncoming(communication, NullLoggerFactory.Instance.CreateLogger<HttpServerDataPortIncoming>(), System.Runtime.Loader.AssemblyLoadContext.Default);

        incoming.Received += values =>
        {
            foreach (var channel in values)
                Console.WriteLine(channel.Value);
        };

        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5002/Value");
        request.Content = new StringContent("{\u0022Test\u0022:{\u0022Value\u0022:0.2},\u0022$meta\u0022:{\u0022Test\u0022:{\u0022Value\u0022:\u0022System.Double, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\u0022}}}");

        var result = await httpClient.SendAsync(request);

        Console.ReadLine();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private static HttpClient CreateHttpClient()
        => new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
}
