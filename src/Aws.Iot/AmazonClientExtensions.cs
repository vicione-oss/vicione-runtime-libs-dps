using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

internal static class AmazonClientExtensions
{
    internal static async Task<string> GetEndpointAddress(this IAmazonClient amazonClient, AwsIotDataPortProperties properties, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(properties.EndpointAddress))
            return properties.EndpointAddress;
        return await amazonClient.GetEndpointAddress(cancellationToken);
    }

    internal static async Task EnsureThings(this IAmazonClient amazonClient, IReadOnlyCollection<INode> thingNodes, string certificateId, CancellationToken cancellationToken)
    {
        var principal = await amazonClient.GetCertificateArn(certificateId, cancellationToken);
        var registeredThings = await amazonClient.GetThings(cancellationToken);
        var missingThings = thingNodes.Where(t => !registeredThings.Contains(t.Name)).Select(t => t.Name).ToArray();

        foreach (var thing in missingThings)
            await amazonClient.CreateThing(thing, cancellationToken);

        var thingsWithCertificate = await amazonClient.GetCertificateThings(principal, cancellationToken);
        var thingsWithoutCertificate = thingNodes.Where(n => !thingsWithCertificate.Contains(n.Name)).Select(t => t.Name);

        foreach (var thing in thingsWithoutCertificate)
            await amazonClient.AttachCertificateToThing(thing, principal, cancellationToken);
    }
}
