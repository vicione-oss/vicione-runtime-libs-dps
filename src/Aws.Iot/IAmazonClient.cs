using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Suite.DataPort;

internal interface IAmazonClient
{
    Task<string> GetEndpointAddress(CancellationToken cancellationToken);
    Task<string> GetCertificateArn(string certificateId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetThings(CancellationToken cancellationToken);
    Task CreateThing(string thingName, CancellationToken cancellationToken);
    Task AttachCertificateToThing(string thingName, string principal, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetCertificateThings(string principal, CancellationToken cancellationToken);
}
