# Migrating Mermaid/ASCII Diagrams to Excalidraw

One-time playbook for converting a repo's existing Mermaid (and ASCII-art)
diagrams to the Excalidraw setup described in
[diagrams-howto.md](diagrams-howto.md). Portable: copy this
`docs/diagram-creation/` folder into the target repo.

## 1. Inventory

- Find the diagrams: `grep -rn '```mermaid' docs` — count per file and note the
  types (graph/sequence/state/class); sequence and state diagrams are the most
  work to translate.
- Directory trees and imported reference material often *look* like ASCII
  diagrams — leave them as text.
- Decide scope deliberately: GitLab renders Mermaid natively, so converting is
  a trade of native rendering for the Excalidraw look and editability.

## 2. Author the .excalidraw files

The files are plain JSON and can be hand-written or AI-generated (one agent per
doc parallelizes well). Top-level shape:

```json
{"type":"excalidraw","version":2,"source":"https://excalidraw.com",
 "elements":[],"appState":{"gridSize":null,"viewBackgroundColor":"#ffffff"},"files":{}}
```

Element essentials (Excalidraw fills in missing fields on load):

- **Box**: `rectangle` with `boundElements:[{"id":"<textId>","type":"text"}]`;
  label is a `text` element with `containerId` set to the box id.
- **Arrow**: `points:[[0,0],[dx,dy]]`, `startBinding`/`endBinding` =
  `{"elementId":"<id>","focus":0,"gap":4}` (or `null` for free arrows);
  label via a bound text like boxes. Dashed = `"strokeStyle":"dashed"`.
- **Subgraph/frame**: a light-gray rectangle listed *first* (renders behind),
  with a free-standing label text (`containerId: null`).
- Unique `id` per element; `seed`/`versionNonce` any positive ints;
  `fontFamily: 1` = hand-drawn (Virgil), `2` = normal, `3` = code.

Layout rules of thumb: box width ≥ 160 px (~9 px per label character + 30),
height 50–60, gaps ≥ 60/80 px, nothing overlaps. Sequence diagrams: participant
boxes in a top row, dashed vertical lifelines (`line` elements), horizontal
labeled message arrows top-to-bottom (y-step ~70), returns dashed, alt/loop
frames as thin transparent rectangles with a small label. Color roles keep
diagrams consistent: blue `#a5d8ff` components, green `#b2f2bb` framework/base,
yellow `#ffec99` configuration, purple `#d0bfff` external/native/PLC,
red `#ffc9c9` errors, gray `#f1f3f5` containers.

Be faithful to the source diagram: every node, edge, and label must appear;
invent nothing.

## 3. Validate the generated JSON

Hand-authored JSON needs structural validation — parses, unique ids, every
reference resolves:

```python
import json, glob
for p in glob.glob("docs/*/diagrams/*.excalidraw"):
    d = json.load(open(p)); els = {e["id"] for e in d["elements"]}
    assert len(els) == len(d["elements"]), p
    for e in d["elements"]:
        if e.get("containerId"): assert e["containerId"] in els, p
        for b in (e.get("startBinding"), e.get("endBinding")):
            if b: assert b["elementId"] in els, p
```

## 4. Replace the Mermaid blocks

Replace each fenced ` ```mermaid ` block with a single image line referencing
the future export: `![Title](diagrams/<name>.svg)`. Change nothing else in the
docs.

## 5. Export and fix fonts

Open each `.excalidraw` in the IDE plugin and export the SVG next to it (same
basename). Then run `dotnet run docs/diagram-creation/fix-excalidraw-fonts.cs` — fresh
exports always carry the remote-font problem (details in
[diagrams-howto.md](diagrams-howto.md)).

## 6. Final verification sweep

- No ` ```mermaid ` blocks remain in the converted docs.
- Every embedded `.svg` path has a matching `.excalidraw`; every `.excalidraw`
  has an exported `.svg` (exports trickle in — re-check at the end).
- No `excalidraw.com` references remain in any SVG (font fix applied).
- Solution/index files list the new diagram files and no deleted ones.
