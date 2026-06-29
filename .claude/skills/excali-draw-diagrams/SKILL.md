---
name: excali-draw-diagrams
description: Use when generating, reviewing or editing Excalidraw diagrams as JSON (.excalidraw files or scene JSON). Applies this repo's diagram stencil standard (palette, fonts, arrows).
---

# Project diagram standard (this repo)

When generating or editing diagrams under `docs/`, match the shared stencil
library — `docs/diagram-creation/diagram-stencils.excalidrawlib`. Full
catalog & rationale: the *Stencil library & style standard* section of
`docs/diagram-creation/diagrams-howto.md`. Stay within the palette and sizes below; do **not**
introduce off-palette tints or extra font sizes.

**Shared defaults (every element):** `fillStyle: "solid"`, `roughness: 1`, font
**Virgil** (`fontFamily: 1`), text colour `#1e1e1e` (`#495057` for secondary
text / captions). Shapes: `strokeWidth: 2`, `roundness: { "type": 3 }`.
Connectors: `roundness: { "type": 2 }`.

**Boxes** — `rectangle`, stroke `#1e1e1e`, width 2, solid, rounded; fill by role:

| Role                  | Fill                |
|-----------------------|---------------------|
| Primary component     | `#a5d8ff` (blue)    |
| Active / data         | `#b2f2bb` (green)   |
| Config / intermediate | `#ffec99` (yellow)  |
| Error / failure       | `#ffc9c9` (red)     |
| External              | `#d0bfff` (violet)  |
| Neutral container     | `#f1f3f5` (gray)    |

Muted / secondary box: stroke `#ced4da`, fill `#f1f3f5`, **width 1**.

**Decision:** `diamond`, stroke `#1e1e1e`, fill `#f1f3f5`, width 2, rounded.

**Lifecycle nodes:** start = filled black circle (`ellipse`, stroke + fill
`#1e1e1e`, sharp); end = final state (transparent ring + inner filled black
dot, sharp).

**Connectors** — `arrow`, width 2, curved, `endArrowhead: "arrow"` (never
`triangle`):

- flow: stroke `#1e1e1e`, solid
- return / async: stroke `#1e1e1e`, dashed
- error: stroke `#e03131`, solid
- muted / optional: stroke `#868e96`, dashed
- separator: `line`, stroke `#1e1e1e`, width 2, dashed, sharp

**Type scale (Virgil):** **16** title · **14** body · **13** small / caption.

The binding rules below still apply to every shape you place.

# Excalidraw Bindings

Excalidraw bindings are **bidirectional**. A binding only works if BOTH sides reference each other. Setting one side and omitting the other produces broken or non-reflowing bindings. This is the single most common mistake — fix it by always writing both sides.

## How bindings behave (read this first)

`boundElements` is a **reverse index** — a "which elements reference me" list on a shape. It carries no behavior itself; it just lets Excalidraw know what to update when the shape moves, resizes, or is deleted. The actual behavior is driven by the *other* side: the text's `containerId` or the arrow's `startBinding`/`endBinding`. This is why a missing back-reference breaks things silently — the element still renders once, but Excalidraw never knows to update it, because it's not in the index it consults.

Despite sharing the `boundElements` list, the two binding `type`s are **different relationships**:

**Containment — shape ↔ text** (`type: "text"`): the text lives *inside* the shape. Move or resize the shape and the text moves, re-wraps, and re-centers with it; if the text is too tall, the *shape* auto-grows to fit. A shape holds **at most one** bound text. **Deleting the shape deletes its text.** The text is not independently selectable or draggable.

**Attachment — shape ↔ arrow** (`type: "arrow"`): the arrow is an *independent* element that merely tracks the shape's edge at the bound endpoint (placed by `focus` and `gap`). Move or resize the shape and the bound endpoint recomputes to follow. A shape can have **any number** of bound arrows. **Deleting the shape does NOT delete the arrow** — its binding is dropped and that end becomes free-floating where it last sat. Dragging a bound endpoint away also unbinds it.

The key asymmetry: **containment cascades on delete, attachment does not.** When editing or deleting elements, account for this — removing a shape leaves dangling arrows that need their bindings cleaned up, whereas bound text should never be deleted separately from its container.

## Rule 1: Arrows must be bound on both sides

An arrow that should follow shapes when they move needs:

1. `startBinding` / `endBinding` on the **arrow**, pointing at the shape IDs.
2. A matching entry in `boundElements` on **each shape**, pointing back at the arrow ID.

Both are required. Omitting `boundElements` on the shape is what breaks reflow.

```json
// Arrow element
{
  "id": "arrow1",
  "type": "arrow",
  "x": 100, "y": 50,
  "width": 200, "height": 0,
  "points": [[0, 0], [200, 0]],
  "startBinding": { "elementId": "box-a", "focus": 0, "gap": 4 },
  "endBinding":   { "elementId": "box-b", "focus": 0, "gap": 4 }
}
```

```json
// Source shape
{
  "id": "box-a",
  "type": "rectangle",
  "boundElements": [ { "id": "arrow1", "type": "arrow" } ]
}
// Target shape
{
  "id": "box-b",
  "type": "rectangle",
  "boundElements": [ { "id": "arrow1", "type": "arrow" } ]
}
```

Binding field guidance:
- `focus`: number roughly in `[-1, 1]`, the attachment point along the shape edge. `0` = center, which is the safe default. Excalidraw recalculates the exact edge point on render, so an approximate value is fine.
- `gap`: pixel gap between arrow tip and shape edge. `2`–`8` is typical; use `4`.
- The arrow's `points` and `x`/`y` only need to be roughly correct (start near the source, end near the target). Once bound, Excalidraw recomputes geometry on load and whenever a shape moves. Do not agonize over exact coordinates.

## Rule 2: Default shape labels to bound text, not free-floating text

When text labels a shape, bind it to that shape by default. Excalidraw has first-class container text: a `text` element bound to a container reflows, re-centers, and wraps automatically with the container, so the label stays correct when the shape moves or resizes. Reach for bound text first; only use a separate free-floating `text` element in the rare cases below where containment would get in the way.

A labeled shape requires:

1. A `text` element with `containerId` set to the shape's ID, and `verticalAlign: "middle"` (centers it).
2. A `boundElements` entry of `type: "text"` on the container shape.

```json
// Container shape
{
  "id": "box-a",
  "type": "rectangle",
  "boundElements": [
    { "id": "label-a", "type": "text" }
  ]
}
```

```json
// Bound text element
{
  "id": "label-a",
  "type": "text",
  "text": "Order Service",
  "containerId": "box-a",
  "textAlign": "center",
  "verticalAlign": "middle"
}
```

`boundElements` on a shape can hold both text and arrows together:

```json
"boundElements": [
  { "id": "label-a", "type": "text" },
  { "id": "arrow1",  "type": "arrow" }
]
```

A free-floating `text` element (one with `containerId: null`) is the right choice only in special cases, such as: diagram-level annotations that belong to no shape (a title, a legend, a standalone note); a label that must sit *outside* its shape (e.g. above or beside it) rather than centered within it; a caption spanning or pointing at several shapes at once; or text on a shape type that can't act as a text container. Outside cases like these, default to bound text.

## Pre-output checklist

Before emitting the JSON, verify:

- [ ] Styles match the **project diagram standard** (palette fills, `strokeWidth` 2, Virgil font, 16/14/13 type scale, curved arrows with `endArrowhead: "arrow"`).
- [ ] Every arrow has `startBinding` AND `endBinding` (unless intentionally free-floating).
- [ ] Every bound arrow's two shapes each list that arrow in their `boundElements`.
- [ ] Shape labels default to bound text (`containerId` set); a free-floating `text` over a shape is used only for a deliberate special case.
- [ ] Every shape-label `text` element has a matching `type: "text"` entry in its container's `boundElements`.
- [ ] All `id` references resolve to an element that actually exists in the scene.
- [ ] Required top-level fields are present: `{ "type": "excalidraw", "version": 2, "source": "...", "elements": [...], "appState": {...}, "files": {} }`.

## Common failure modes

- **Arrow doesn't reflow when shape moves** → missing `boundElements` back-reference on the shape. Both sides required.
- **Label drifts off the shape when shape moves** → label was a free-floating `text` instead of bound text with `containerId`.
- **Binding silently dropped on import** → an `elementId` / `id` references an element not present in `elements`.
- **Text not centered in shape** → missing `verticalAlign: "middle"` on the bound text element.
- **Dangling arrow after deleting a shape** → arrows aren't deleted with their shape. On delete, either drop the arrow too or clear its `startBinding`/`endBinding` for that endpoint; also remove the arrow's entry from any surviving shape's `boundElements`.
- **Orphaned text after deleting a shape** → bound text should be removed when its container is, not left behind with a `containerId` pointing at a now-missing shape.
