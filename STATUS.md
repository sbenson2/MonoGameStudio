# MonoGameStudio — Status & Roadmap

> Last updated: 2026-02-25

Personal 2D game editor built with MonoGame + Arch ECS + ImGui.
Roadmap milestones derived from the [Universal 2D Engine Toolkit](../INDEX.md) knowledge base (41 docs).

---

## Current State (v0.1 — Editor Foundation)

**Build**: `dotnet run --project src/MonoGameStudio.Desktop` — .NET 10, builds and runs clean.

### What Works

#### Core (32 files)
- [x] Arch ECS integration — entity CRUD, hierarchy, parent/child with circular-parenting prevention
- [x] 19 component types: transforms (5), hierarchy (3), metadata (5 — incl. EntityTag), rendering (4), physics (3), audio (1), UI (1)
- [x] 4 systems: TransformPropagation, SpriteRendering, Animation, GumUI
- [x] Scene serialization — two-pass JSON with GUID-based parent linking
- [x] Component registry — typed descriptors, category filtering, no reflection
- [x] Sprite sheet data format (`.spritesheet.json`) + animation data format (`.animation.json`)
- [x] Texture cache, logging (circular buffer, 1024 max), project file I/O

#### Editor (50 files)
- [x] 11 ImGui panels: Menu, Toolbar, Hierarchy, Inspector, Viewport, Console, AssetBrowser, SpriteSheet, Animation, StartScreen, Settings + About dialog
- [x] Viewport rendering — camera pan/zoom, grid, entity markers
- [x] Gizmo tools — Move, Rotate, Scale with hit detection and drag
- [x] Selection — click, box select, Ctrl multi-select
- [x] Undo/Redo — 8 command types (Move, MoveMultiple, Create, Delete, Duplicate, Rename, AddComponent, RemoveComponent), 100-deep stack
- [x] Play mode — scene snapshot/restore
- [x] Asset browser — folder tree, grid/list view, search, type filters, thumbnails, FileSystemWatcher auto-refresh, drag-drop source
- [x] Sprite sheet editor — texture preview, auto-slice, frame rect editing, zoom
- [x] Animation editor — timeline, clip tabs, playback preview, frame properties
- [x] Start screen — new project wizard, recent projects, templates (Empty, 2D Platformer, Top-Down RPG)
- [x] Project management — create, open, close, save; `.mgstudio` files; `dotnet new mgdesktopgl` scaffolding
- [x] Inspector — component drawer with field widgets (float, int, Vector2, Color, Rectangle, etc.), Add Component picker with category grouping
- [x] Keyboard shortcuts (Ctrl+S/N/O, Ctrl+Z/Y, Ctrl+C/V, Q/W/E/R for gizmo modes)
- [x] Copy/paste entities (Ctrl+C/V) with full component data via ClipboardManager
- [x] Prefab system — save as `.prefab.json`, drag-drop to instantiate from asset browser
- [x] Multi-select transform — group move via MoveMultipleEntitiesCommand
- [x] Entity search/filter bar in hierarchy panel
- [x] Entity tags (EntityTag component)
- [x] Collider visualization — wireframe outlines for BoxCollider/CircleCollider in viewport
- [x] Virtual resolution preview — settings panel + viewport letterbox/pillarbox overlay
- [x] Drag-drop assets onto viewport to create sprite entities
- [x] Drag-drop asset paths into string fields in inspector
- [x] macOS native integration — menu bar, title bar, file dialogs, toolbar
- [x] Docking layout with profile save/load
- [x] DPI-aware font loading (Inter, JetBrains Mono, FontAwesome)

#### Desktop (1 file)
- [x] Thin launcher — `new EditorGame(args).Run()`

### Known Quirks
See CLAUDE.md "Known Quirks" section — 14 documented workarounds for Hexa.NET.ImGui, MonoGame, Arch ECS, and macOS interop.

---

## Roadmap

Milestones derived from the Universal 2D Engine Toolkit docs. Each version targets a single theme.

### v0.2 — Sprite Workflow Polish
> Sources: G8 (Content Pipeline), G2 (Rendering & Graphics)

- [x] Drag-drop sprites from asset browser onto viewport to create entities
- [x] Drag-drop sprite sheets onto SpriteRenderer in inspector
- [ ] Atlas/texture packing (combine loose sprites into atlas)
- [ ] Sprite import settings (filter mode, wrap mode)
- [ ] Sprite origin/pivot editor in sprite sheet panel

### v0.3 — Scene Editing QoL
> Sources: G1 (Custom Code Recipes), G29 (Game Editor)

- [x] Multi-select transform (move/rotate/scale group)
- [x] Copy/paste entities (Ctrl+C/V with hierarchy)
- [ ] Duplicate shortcut (Ctrl+D)
- [x] Prefab system — save entity hierarchy as reusable `.prefab.json`
- [x] Scene search/filter in hierarchy panel
- [x] Entity tags and layers

### v0.4 — Custom Code Integration
> Sources: G1 (Custom Code Recipes), G15 (Game Loop), G18 (Game Programming Patterns)

- [ ] Scene manager (load/unload/transition between scenes)
- [ ] Render layers / sort layers configurable in editor
- [ ] Tween system integration
- [ ] Screen transitions (fade, slide, etc.)
- [ ] Timer / coroutine system
- [ ] Scriptable component support (user-defined components from game project)

### v0.5 — Physics & Collision Visualization
> Sources: G3 (Physics & Collision)

- [x] Collider outlines drawn in viewport (BoxCollider, CircleCollider)
- [ ] Collider editing gizmos (drag handles to resize)
- [ ] Aether Physics 2D integration
- [ ] Collision layer matrix editor in project settings
- [ ] Physics debug overlay (contacts, raycasts)

### v0.6 — Tilemap Editing
> Sources: G28 (Top-Down Perspective), G8 (Content Pipeline)

- [ ] Tilemap editor panel — tileset palette, paint/erase/fill tools
- [ ] Paint tiles directly in viewport
- [ ] Auto-tiling rules (bitmask-based)
- [ ] Tiled `.tmx` import
- [ ] Tile collision shapes
- [ ] Multiple tilemap layers

### v0.7 — Camera & Display
> Sources: G19 (Display & Resolution), G20 (Camera Systems), G21 (Coordinate Systems), G24 (Window Management)

- [x] Virtual resolution preview in viewport (letterbox/pillarbox visualization)
- [ ] Camera system configuration in inspector (follow target, deadzone, lookahead)
- [ ] Multi-camera support
- [ ] Viewport safe area overlay (G25)
- [ ] Resolution preset selector

### v0.8 — Particle System
> Sources: G23 (Particles)

- [ ] Particle editor panel — emitter properties, curves, preview
- [ ] Particle emitter component
- [ ] Emission patterns (point, circle, rectangle, edge)
- [ ] Particle rendering in viewport
- [ ] Save/load particle presets

### v0.9 — Audio & Shaders
> Sources: G6 (Audio), G27 (Shaders & Visual Effects)

- [ ] Audio clip preview in asset browser (play/stop)
- [ ] AudioSource 3D positioning preview in viewport
- [ ] Shader/effect preview panel
- [ ] Material editor (shader + parameter assignment)
- [ ] Post-processing stack configuration

### v1.0 — Game Runtime Bridge
> Sources: G29 (Game Editor), E4 (Project Management)

- [ ] Build & run game project from editor
- [ ] Console output piping (game stdout → editor console)
- [ ] Hot reload (detect recompile, reload assemblies)
- [ ] Game viewport (play in editor window)
- [ ] Build profiles (Debug/Release, platform targets)

### Future (unscheduled — driven by game needs)
> Sources: G4 (AI), G9 (Networking), G10 (Custom Game Systems), G22 (Parallax)

- [ ] AI behavior tree / state machine editor — G4
- [ ] Networking inspector (packet viewer, latency sim) — G9
- [ ] Procedural generation preview tools — G10
- [ ] Parallax layer editor — G22
- [ ] Input binding editor — G7
- [ ] UI layout editor (Gum visual tool integration) — G5

---

## Active TODOs

Short-term tasks for the current session.

- [x] Implement Help > About dialog
- [x] First git commit of current working state

---

## Decision Log

Newest first.

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-25 | Feature push: prefabs, copy/paste, collider viz, multi-select, search, tags, drag-drop, virtual resolution, About dialog | Batch of QoL features pulled forward from v0.2–v0.7 roadmap. Completes v0.1 and partially fulfills v0.2, v0.3, v0.5, v0.7. |
| 2026-02-25 | Created STATUS.md as single-file progress tracker | Solo dev — markdown beats ticket management. Sections can migrate to GitHub Issues later. |
| 2026-02-24 | Abandoned native SwiftUI/NativeAOT rearchitecture | Tool-building trap. Deleted MonoGameStudio.Native and MonoGameStudio.macOS. ImGui is industry-proven for internal tools (Blizzard, Rockstar, id Software, Valve). Focus on making games. |
| 2026-02-24 | Chose Hexa.NET.ImGui over ImGui.NET | Active development, better .NET 10 support, native docking/tables. Required migration from ImGui.NET API patterns. |
| 2026-02-24 | Arch ECS over custom ECS | Arch 2.1.0 is fast, well-maintained, C#-native. Custom EntityRef wrapper handles safe entity storage. |
