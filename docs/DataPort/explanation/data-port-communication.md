# DataPortCommunication — Design Rationale

`DataPortCommunication` is the object the ViciOne platform constructs at runtime and passes to each DataPort implementation's constructor. Understanding what it contains and why it is shaped the way it is helps explain all the mapping work that follows.

---

## Why it exists

The platform that hosts DataPort implementations must be protocol-agnostic. It cannot know whether a given DataPort connects to an OPC-UA server, an MQTT broker, or a SQL database. It must provide a general-purpose container that every protocol can receive, regardless of what that protocol considers a "data point" or how it organises its configuration hierarchy.

`DataPortCommunication` is that container. It carries:

- A **name** — the configured name of this DataPort instance
- A **flat collection of `Node` objects** — the user's configured data points, represented in a protocol-neutral, weakly-typed structure

The flat collection is what makes `DataPortCommunication` protocol-agnostic. The platform neither knows nor cares whether the nodes represent OPC-UA address space items, MQTT topics, or database columns. Each node carries a `DesignId` that the protocol-specific code uses to determine how to interpret it, and a `Properties` dictionary that holds whatever the protocol needs.

---

## DataPortCommunication subclasses

Protocol implementations define a concrete subclass of `DataPortCommunication`. The subclass typically adds no new fields — the `Nodes` collection carries the entire configuration hierarchy. The subclass exists because the platform uses the concrete type to deserialize the right stored configuration, and because `TypedNodeMapper` in Extensions is parameterised on the `DataPortCommunication` subclass type. Some protocols add top-level properties (connection string, endpoint URL) when those are not part of the node hierarchy; that is a per-protocol decision.

The subclass is bound to the protocol's **root identifier** — the root design id declared in the YAML schema — with a `[Communication("<root-id>")]` attribute. On deploy, the managed engine instantiates the port and hands in a `DataPortCommunication` instance representing the user's concrete configuration for one concrete external system.

From then on the managed engine drives the port through the matching communication contract, parameterised on the subclass type:

- `IExternalIncomingCommunication<TCommunication>` for reading ports
- `IExternalOutgoingCommunication<TCommunication>` for writing ports

> **Design note.** This contract is deliberately small and generic — small enough that a few of its steps quietly carry more than they advertise — so it may be worth widening and documenting more fully in the future.

---

## The Node model

Each `Node` in the flat collection represents one configured element in the user's data point hierarchy. Three aspects of `Node` deserve explanation at the conceptual level:

**`DesignId`** is the schema-defined type identifier — a string constant from the protocol's YAML schema that names what kind of node this is (e.g., `"integer-register"`, `"opc-ua-folder"`, `"mqtt-topic"`). This is what protocol-specific mappers use for dispatch: `IDataPointNodeMapper.IsTargetMapperFor` compares the node's `DesignId` against the mapper's known constant to claim or reject a node. Every other field is generic; `DesignId` is the protocol-specific key.

**`Properties`** is a `Dictionary<string, Property>` carrying all per-node configuration values — addresses, type hints, range limits, encoding choices, whatever the protocol needs. It is intentionally weakly typed (see [Why weakly typed](#why-weakly-typed) below), and can be nested arbitrarily for structured configuration.

**`AffectedChannels` and `TransferredChannels`** declare the node's relationship to ViciOne's data-flow routing. These are explained fully in [LinkedNode and channels](linked-node.md); the short version is that `AffectedChannels` marks leaf nodes (direct data sources/sinks) and `TransferredChannels` marks all nodes that participate in a data flow, including container/group nodes.

The remaining fields — `Id`, `ParentId`, `Name`, `ValueType` — are structural and bookkeeping. `Id` and `ParentId` encode the hierarchy in the flat collection; `LinkedNodeFactory` uses them to reconstruct parent-child object references.

---

## The Property model

Properties are the per-node configuration values. A `Property` wraps two things:

- **`Value`** — the actual value, typed as `object`. At runtime it will be a primitive (string, int, bool, double) or an `IDictionary<string, Property>` for nested structures.
- **`Channels`** — the list of channel strings associated with this property. Some properties are themselves channel-bound, meaning that when a channel value arrives, the property changes.

Properties can nest arbitrarily. A node's `Properties` dictionary may contain a key whose value is itself a dictionary of properties, which may contain further nested dictionaries. This is why `NodePropertiesExtensions.GetProperty` accepts dot-separated paths like `"connection.timeout"` — it traverses the nesting levels recursively.

---

## Why weakly typed

The `Properties` dictionary holds `object` values. The platform serializes and deserializes property values without knowing the target types — that knowledge lives in the protocol-specific mapper code. This is intentional: introducing a protocol's types into the platform would create a hard dependency in the wrong direction.

The type information re-enters the system at the mapper level. In Extensions, `IDataPointNodeMapper.Map` and the `LinkedNodeExtensions` helper methods extract typed values from the raw properties, throwing `InvalidPropertyException` if a required property is absent or has the wrong type. This is also where the YAML consistency tests catch regressions — they verify that the property names the mapper expects match the property names documented in the YAML schema file.

---

## Related

- [LinkedNode and channels](linked-node.md) — what happens after the flat `Nodes` collection is received
- [Typed node model](../../DataPort.Extensions/explanation/configuration/typed-node-model.md) — how Extensions maps LinkedNodes to a protocol-specific typed configuration tree
- [Overview](overview.md) — where DataPortCommunication sits in the wider system
