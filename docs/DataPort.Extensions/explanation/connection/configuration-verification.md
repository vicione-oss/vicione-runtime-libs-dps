# Configuration Verification: Design Rationale

Both base classes support an optional startup step that verifies the configured data point addresses against the live device. It runs after the connection is established but before polling or write processing begins.

## Why it runs at startup, not construction

The library's guiding principle is that every error should surface at the earliest phase that can detect it (see [Overview](../Overview.md)). For most configuration problems that phase is construction, before the network is ever touched. Address verification is the one exception, because it is the only check that cannot be made without a live system to ask. At construction there is no connection yet, so there is nothing to verify an address against. Startup is the first moment a real device is on the other end, which makes it the earliest the check can run at all.

## Why verification is opt-in

Not all protocols expose metadata about whether an address exists. For some, the only way to find out is to attempt a full read, which costs as much as polling the address would. Making the check mandatory would impose that cost on every protocol, including the ones it cannot help. So verification is opt-in: a port overrides `CreateConfigurationVerifier` to supply one, and the default supplies none, which skips the step.

## What a failure does

When a verifier is present, it checks the configured addresses against the live device, and the first one that fails aborts startup. The failure surfaces as `InvalidConfigurationException`, the base class disconnects and releases the client it had just acquired, and the port is left cleanly disconnected rather than half-started. Nothing has polled or written by this point, so the failure is loud and early.

The reason that is worth doing is that the alternative is silent. Without the check, a misconfigured address never announces itself: the port comes up looking healthy while a handful of its data points quietly never produce a value. Verification turns a latent data gap into an obvious, up-front failure.

## Why it is a virtual factory method

`CreateConfigurationVerifier` is a virtual factory method rather than a constructor parameter because a verifier needs the connected protocol client to do its work, and that client only exists once `AcquireConnectedAsync` has completed inside `ConnectAsync`. The base class calls `CreateConfigurationVerifier(client)` right after acquiring the connection and before any background work starts.

---

## Related

- [Connection lifecycle](connection-lifecycle.md): why a verification failure disconnects before throwing, so a failed startup never leaks a live connection
- [Client lifecycle manager](client-lifecycle-manager.md): why the verifier needs the connected client, which is only available after acquisition
- [Overview](../Overview.md): the lifecycle phases and where startup sits among them
- [Incoming data port](../incoming/incoming-data-port.md): lifecycle diagram showing where verification fits
- [Outgoing data port](../outgoing/outgoing-data-port.md): lifecycle diagram showing where verification fits
