namespace ViciOne.Suite.DataPort.Extension.Tests.Outgoing;

/// <summary>
/// Minimal test-only client type (no implementation dependencies).
/// </summary>
#pragma warning disable CA1515 // types are used by NSubstitute and cannot be avoided
public class TestClient
#pragma warning restore CA1515 // types are used by NSubstitute and cannot be avoided
{
    public bool IsConnected { get; set; }
    public int ConnectionCount { get; set; }
}
