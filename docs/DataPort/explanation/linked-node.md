# LinkedNode and Channels — Design Rationale

`LinkedNode` and the channel system are the two concepts that bridge the flat, weakly-typed `DataPortCommunication.Nodes` collection and the protocol-specific work that follows. This document explains why they are designed the way they are.

---

## Why LinkedNode exists

`DataPortCommunication.Nodes` is a flat `IReadOnlyCollection<Node>`. The hierarchy is encoded by `ParentId` — each node carries a nullable GUID pointing to its parent. This is the serialization-friendly representation: it is simple to persist, diff, and deserialize without knowing the protocol.

But working with a flat collection when you need hierarchical relationships is unwieldy. To derive a device address from a node, you typically need its entire ancestry — device → area → tag. To walk all descendants of a group, you need to find all nodes whose `ParentId` chains back to it.

`LinkedNode` solves this by wrapping a `Node` and adding `Parent` and `Children` object references:

```
Node (flat, ParentId only)           LinkedNode (linked, object refs)
──────────────────────────           ────────────────────────────────
Node { Id: A, ParentId: null }  →    LinkedNode(A) { Parent: null, Children: [B, C] }
Node { Id: B, ParentId: A    }  →    LinkedNode(B) { Parent: A,    Children: [D]    }
Node { Id: C, ParentId: A    }  →    LinkedNode(C) { Parent: A,    Children: []     }
Node { Id: D, ParentId: B    }  →    LinkedNode(D) { Parent: B,    Children: []     }
```

`LinkedNodeFactory.Create` builds this linked tree from the flat collection in a single pass. After that, upward and downward traversal are simple object navigations — no dictionary lookups needed.

This is the `LinkedNode` that Extensions mappers and validators receive. The tree structure mirrors the user's configured hierarchy in ViciOne Studio, which in turn mirrors the device or protocol's own hierarchy (OPC-UA namespace, database schema, MQTT topic tree, etc.).

---

## The channel system

Channels are ViciOne's named data pipes. A channel is identified by a string. Data flows between components via these named channels — an incoming DataPort publishes values to channels, the engine distributes them, and other components (including outgoing DataPorts) receive values from channels.

On top of the configured node tree, users define **dataflows** that decide which values move between the engine and an external system. A DataPort never sees a dataflow itself: what reaches it at runtime is a **channel** on each value — a hierarchical identifier that points back at an object in the configured tree.

Nodes declare their relationship to channels through two properties:

### AffectedChannels

`AffectedChannels` lists the channels this node directly maps to. A node with affected channels is a **leaf node** — it is a concrete data point that reads from or writes to a specific device address. The node's channels are the channels the DataPort will publish to (incoming) or receive from (outgoing).

### TransferredChannels

`TransferredChannels` lists the channels that are routed *through* this node. Every node that participates in a data flow has transferred channels. For leaf nodes, `TransferredChannels` and `AffectedChannels` are typically the same set. For group nodes, `TransferredChannels` aggregates the channels of all descendant leaf nodes.

### Group nodes

A node where `AffectedChannels.Count == 0` is a **group node**. It has no direct channel mapping itself; it is a structural container for related leaf nodes beneath it. Group nodes exist for protocol-level reasons — some protocols have hierarchical addressing where the full address is assembled by concatenating the route from root to leaf, or where multiple values arrive together in one protocol message and must be dispatched to individual channels.

```
LinkedNode: Device                    AffectedChannels:   []
    │                                 TransferredChannels: ["ch.A", "ch.B", "ch.C"]
    │
    ├─▶ LinkedNode: Area              AffectedChannels:   []          ← group node
    │       │                         TransferredChannels: ["ch.A", "ch.B"]
    │       │
    │       ├─▶ LinkedNode: Tag1      AffectedChannels:   ["ch.A"]   ← leaf node
    │       │                         TransferredChannels: ["ch.A"]
    │       │
    │       └─▶ LinkedNode: Tag2      AffectedChannels:   ["ch.B"]   ← leaf node
    │                                 TransferredChannels: ["ch.B"]
    │
    └─▶ LinkedNode: Tag3              AffectedChannels:   ["ch.C"]   ← leaf node
                                      TransferredChannels: ["ch.C"]
```

Leaf nodes own their channels directly (`AffectedChannels` is set). Group and structural nodes aggregate their descendants' channels in `TransferredChannels` but own none directly (`AffectedChannels` is empty).

The distinction matters for routing:

- **Incoming**: to derive the device address for a read call, the code walks the route from root to the node carrying channels, using the names of each ancestor in sequence. Group nodes contribute a segment of the address; leaf nodes contribute the final segment.
- **Outgoing**: writes arrive per channel. For leaf nodes, the channel maps directly to a single write operation. For group nodes, multiple channel values may be collected and written together in one protocol message.

---

## Why the flat storage, linked at use time

Keeping nodes flat in storage and linking them at use time is a deliberate separation of concerns:

- **Storage and transport** use the flat `ParentId` form — simple to serialize, easy to diff, no object graph serialization complexity.
- **Processing** uses the linked form — tree traversal is natural and efficient.

`LinkedNodeFactory.Create` is the bridge. It is called once, at DataPort startup, when a `DataPortCommunication` is received. The resulting `LinkedNode` tree is then either passed to a `TypedNodeMapper` (Extensions path) or used directly with the `NodeRouteExtensions` and `InitializeUnspecificTree` utilities (non-Extensions path).

---

## Related

- [DataPortCommunication](data-port-communication.md) — the flat `Nodes` collection that LinkedNode is built from
- [Typed node model](../../DataPort.Extensions/explanation/configuration/typed-node-model.md) — how Extensions maps LinkedNodes to a typed configuration tree
- [Internal data model](../../DataPort.Extensions/explanation/configuration/internal-data-model.md) — how the channel concepts above map to the runtime polling and write dispatch model
