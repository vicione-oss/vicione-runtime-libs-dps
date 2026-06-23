---
title: Building a DataPort
---

# Building a DataPort

These guides are recipes for building a protocol-specific data port on top of `IncomingDataPortBase`
and `OutgoingDataPortBase`. They assume you already know *what* a data port is and *why* the base
classes are shaped the way they are — if you don't, read the [Overview](../explanation/Overview.md)
first and come back.

A data port is built in two layers. First you build a set of **shared building blocks** that are the
same whether the port reads or writes: the configuration schema, the typed node model, the runtime
data points, and the connection. Then you add the **direction-specific** pieces — a read client and a
polling port, or a write client and a queuing port. The shared blocks each get their own guide so
both directions can link to them instead of repeating them.

## The worked example

Every snippet in these guides is taken from a complete, compiling example that lives in the test
project at `tests/DataPort.Extensions.Tests/Stubs/`. It implements a fake `Test` protocol as both an
incoming and an outgoing port, and it deliberately shares one `TestCommunication`, one
`TestDeviceNode` tree, one `TestClient`, and one `TestClientLifecycleManager` between the two
directions — the same split these guides describe. Read it alongside the guides; it is the real,
test-covered version of the abstract types below.

> All file paths in these guides are relative to the repository root. Type names link to the
> generated API where a precise signature helps.

## Shared building blocks

Build these once, regardless of direction. Each is a separate guide so the incoming and outgoing
recipes can link to it:

| Step | What you build | Key type | Example stub | Guide |
|------|----------------|----------|--------------|-------|
| 1 | The YAML configuration schema the suite editor offers | — | `YamlConsistency/TestDataPort.yaml` | [Define the configuration schema](shared/configuration-schema.md) |
| 2 | The strongly-typed node tree and its mappers | `IRootConfigurationNode`, `IDataPointNode`, `TypedNodeMapper` | `Stubs/Model/Nodes/` | [Model the configuration as typed nodes](shared/node-model.md) |
| 3 | The runtime data points the port reads or writes | `IDataPoint`, `ITypedDataPoint<TValue>` | `Stubs/Model/DataPort/` | [Define the runtime data points](shared/data-points.md) |
| 4 | The connection: how a client is identified, opened, and released | `IClientInformation`, `IClientLifecycleManager` | `Stubs/Client/` | [Manage the connection](shared/connection.md) |

Two further building blocks are optional but recommended:

| Step | What you build | Key type | Example stub | Guide |
|------|----------------|----------|--------------|-------|
| 5 | A check that the configured addresses exist on the live device | `IDataPointConfigurationVerifier<TDataPoint>` | `Stubs/Verification/` | [Verify configuration on connect](shared/configuration-verification.md) |
| 6 | A test that keeps the YAML schema and the C# node model in sync | `YamlConsistencyBaseTest` | `YamlConsistency/YamlConsistencyTest.cs` | [Keep the schema and node model in sync](shared/yaml-consistency-test.md) |

## Then choose a direction

With the shared blocks in place, add the direction-specific pieces:

- **[Build an incoming data port](incoming-dataport/incoming-data-port.md)** — reads the device on a
  schedule and publishes values. You add a read client, a data-point-groups mapper, and a subclass of
  `IncomingDataPortBase`.
- **[Build an outgoing data port](outgoing-dataport/outgoing-data-port.md)** — takes values from the
  engine and writes them to the device through a background queue. You add a write client, a
  data-points mapper, a value-conversion method, and a subclass of `OutgoingDataPortBase`.

A port can do both directions over one connection, the way the example does — build both ports and
hand them the same shared blocks.

## Related

- [Overview](../explanation/Overview.md) — the concepts these recipes apply
- [Interfaces](../reference/interfaces.md) · [Model](../reference/model.md) — the contracts, grouped by role
- [Generated API](../reference/api/index.md) — every public type and member
