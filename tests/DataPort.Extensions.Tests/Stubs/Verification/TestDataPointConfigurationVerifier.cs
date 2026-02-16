using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extensions.Verification;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Verification;

internal sealed class TestDataPointConfigurationVerifier : IDataPointConfigurationVerifier<TestDataPoint>
{
    public List<MisConfiguredDataPoint<TestDataPoint>> VerifyResult { get; set; } = [];

    public ValueTask<List<MisConfiguredDataPoint<TestDataPoint>>> Verify(
        List<TestDataPoint> dataPoints, CancellationToken cancellationToken) =>
        ValueTask.FromResult(VerifyResult);
}
