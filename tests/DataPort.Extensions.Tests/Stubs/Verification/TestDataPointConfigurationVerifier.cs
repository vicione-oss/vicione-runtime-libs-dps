using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Verification;

internal sealed class TestDataPointConfigurationVerifier : IDataPointConfigurationVerifier<TestDataPoint>
{
    public IReadOnlyList<MisconfiguredDataPoint<TestDataPoint>> VerifyResult { get; init; } = [];

    public ValueTask<IReadOnlyList<MisconfiguredDataPoint<TestDataPoint>>> Verify(
        IReadOnlyList<TestDataPoint> dataPoints, CancellationToken cancellationToken) =>
        ValueTask.FromResult(VerifyResult);
}
