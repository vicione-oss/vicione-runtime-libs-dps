# Verify configuration on connect

**Shared building block — optional.** Used by both
[incoming](../incoming-dataport/incoming-data-port.md) and
[outgoing](../outgoing-dataport/outgoing-data-port.md) ports.

Construction-time validation catches *structural* configuration errors, but it can't catch an address
that is well-formed yet doesn't exist on the device — that needs a live connection. Configuration
verification fills the gap: at `ConnectAsync`, after the client is acquired, the base class can check
every configured data point against the live system and fail loudly if any are wrong. Without it, a
misconfigured address becomes a silent data gap — a port that looks healthy while a few of its points
never produce a value.

It is opt-in because not every protocol can check an address cheaply. Supply a verifier only when you
can.

The worked example is `tests/DataPort.Extensions.Tests/Stubs/Verification/TestDataPointConfigurationVerifier.cs`.

## 1. Implement the verifier

`IDataPointConfigurationVerifier<TDataPoint>` has one method, `Verify`, returning the data points that
failed. Report problems by *returning* them, not by throwing: the base class logs each
`MisconfiguredDataPoint` and aborts the connect with `InvalidConfigurationException` when the list is
non-empty. An empty list means everything is valid.

```csharp
internal sealed class TestDataPointConfigurationVerifier : IDataPointConfigurationVerifier<TestDataPoint>
{
    public ValueTask<IReadOnlyList<MisconfiguredDataPoint<TestDataPoint>>> Verify(
        IReadOnlyList<TestDataPoint> dataPoints, CancellationToken cancellationToken)
    {
        // query the live device (via the client you captured) and collect mismatches:
        var misconfigured = dataPoints
            .Where(/* address missing or type wrong on the device */)
            .Select(dp => new MisconfiguredDataPoint<TestDataPoint>(
                dp, [new MismatchingConfiguration("Type mismatch: expected Int32, got String")]))
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<MisconfiguredDataPoint<TestDataPoint>>>(misconfigured);
    }
}
```

Each `MisconfiguredDataPoint` pairs the offending data point with a list of `MismatchingConfiguration`
strings — one human-readable reason per problem, so the configurator gets a specific message rather
than "something is wrong."

## 2. Wire it in via the factory override

You don't pass the verifier to a property directly; you return it from the base class's
`CreateConfigurationVerifier(TClient client)` factory. The client is handed in so the verifier can
query the live device. Returning `null` (the default) skips verification.

```csharp
protected override IDataPointConfigurationVerifier<TestDataPoint>? CreateConfigurationVerifier(TestClient client)
    => new TestDataPointConfigurationVerifier(client);
```

This override lives in your direction-specific subclass — see the
[incoming](../incoming-dataport/incoming-data-port.md) and
[outgoing](../outgoing-dataport/outgoing-data-port.md) guides. On verification failure the base class
disconnects and releases the client before rethrowing, so a rejected configuration never leaves a
connection open.

## Related

- [Configuration verification — design rationale](../../explanation/connection/configuration-verification.md) —
  why it runs at startup not construction, why it's opt-in, and why it's a factory method
- [Interfaces reference](../../reference/interfaces.md#configuration-verification) —
  `IDataPointConfigurationVerifier`
