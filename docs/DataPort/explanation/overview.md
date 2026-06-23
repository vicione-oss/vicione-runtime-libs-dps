# DataPort — Core Concepts

This is the bird's-eye view of the DataPort package: what it provides, where it sits in the ViciOne
system, and how protocol implementations build on it. The detail lives in the dedicated docs linked
throughout — this page only sketches how the pieces fit together.

---

## Purpose

A DataPort is the ViciOne component that bridges the **managed engine** (the ViciOne platform) and one
external device or service. It either reads values from the device and publishes them to the engine
(**incoming**), or receives values from the engine and writes them to the device (**outgoing**).
Implementations are protocol-specific — one for OPC-UA, one for MQTT, one for InfluxDB, and so on.

This package is the shared foundation every implementation builds on. It defines the contract between
the platform and protocol implementations:

- **The communication object** (`DataPortCommunication`) the platform constructs and hands to each
  implementation at startup, carrying the user's configuration.
- **The node model** (`Node`, `Property`) — the weakly-typed structures that hold configured values,
  hierarchy, and channel associations. (What the user configures as a *data point* is represented here
  as a `Node`.)
- **The channel system** — how nodes declare which data flows they participate in.
- **The `LinkedNode` tree** — the navigable form of the node model, built once at startup.

Each is explained in the linked docs below; this overview only relates them to one another.

---

## Where DataPort sits in the system

> _Note: this diagram is still ASCII. To match the rest of the doc set it should be migrated to an
> Excalidraw `.excalidraw` + `.svg` pair (see `docs/diagram-creation/diagrams-howto.md`)._

```
ViciOne Studio (design time)
    Protocol developer defines the YAML configuration schema
    User configures data points against that schema
        │
        ▼
ViciOne platform (runtime)
    Deserializes the stored configuration
    Constructs the communication object
        │
        ▼
DataPort implementation (runtime)
    Receives the communication object and links its nodes into a tree
        │
        ├─▶ Via DataPort.Extensions — a typed configuration model and ready-made base classes
        │
        └─▶ Directly — work with the LinkedNode tree using this package's utilities
```

Everything below "DataPort implementation" is protocol-specific; everything above is platform-provided.
The types in this package define the boundary between them.

The configuration schema is authored against the **ViciOne-YAML schema** (defined in the
[tree-builder repository](https://gitlab.com/vicione-oss/vicione/suite-apps/cluster-management/tree-builder)):
the *dataport-author* declares which concepts and structures of the protocol ViciOne should offer, and what
is required versus optional to configure. A user then configures a *concrete* external system against that
schema in the suite's
[cluster-editor](https://gitlab.com/vicione-oss/vicione/suite-apps/cluster-management/cluster-editor) —
checked only against the schema's **static constraints, with no live connection**, so connection-dependent
validation happens later, at the DataPort's startup phase.

Once constructed, the implementation connects to its device and moves data over **channels** —
publishing read values up to the engine (incoming) or applying received values to the device
(outgoing). The channel model is covered in [LinkedNode and channels](linked-node.md); the runtime
lifecycle (connect, poll, write, resilience) is provided by DataPort.Extensions and explained in the
[Extensions overview](../../DataPort.Extensions/explanation/Overview.md).

---

## Building on DataPort: the two paths

A protocol implementation can be implemented in one of two ways:

- **With DataPort.Extensions** — Extensions is an opinionated sdk to build dataports, which adds a typed configuration
  model and base classes that handle connection lifecycle, polling, write queuing, and resilience.
- **Directly** — the implementation works with the `Node` and `LinkedNode` tree itself, using the
  utilities this package provides (`LinkedNodeFactory`, `NodeRouteExtensions`, and the unspecific-tree
  helpers).

---

## Further Reading

- [DataPortCommunication](data-port-communication.md) — what the communication object carries and where
  it comes from
- [LinkedNode and channels](linked-node.md) — tree reconstruction and the channel system
- [DataPort.Extensions overview](../../DataPort.Extensions/explanation/Overview.md) — how Extensions
  builds on these primitives
