using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.DataPort;
using ViciOne.Suite.DataPort.Extensions.Incoming.Client;
using ViciOne.Suite.DataPort.Extensions.Model.DataPoints;
using ViciOne.Suite.DataPort.Extensions.Outgoing.Client;

namespace ViciOne.Suite.DataPort.Extension.Tests.Stubs.Client;

/// <summary>
/// Minimal test-only client type supporting both incoming and outgoing data ports.
/// </summary>
internal class TestClient
    : IReadClient<TestDataPointGroup, IDataPointValue>, IWriteClient<IDataPointValue>
{
    // Properties for incoming tests
    public IReadOnlyList<IDataPointValue> ReadResult { get; set; } = [];
    public int ReadCallCount { get; private set; }

    // Configurable write behavior for outgoing tests
    public Func<IReadOnlyList<IDataPointValue>, CancellationToken, ValueTask> WriteFunc { get; set; } =
        (_, _) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<IDataPointValue>> ReadAsync(TestDataPointGroup dataPointGroup,
        CancellationToken cancellationToken)
    {
        ReadCallCount++;
        return new ValueTask<IReadOnlyList<IDataPointValue>>(ReadResult);
    }

    public ValueTask WriteAsync(IReadOnlyList<IDataPointValue> values, CancellationToken cancellationToken)
        => WriteFunc(values, cancellationToken);
}
