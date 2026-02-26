# MonoGameStudio — Status & Roadmap

> Last updated: 2026-02-25

Personal 2D game editor built with MonoGame + Arch ECS + ImGui.
Roadmap milestones derived from the [Universal 2D Engine Toolkit](../INDEX.md) knowledge base (41 docs).

---

## Current State

**Build**: `dotnet run --project src/MonoGameStudio.Desktop` — .NET 10, builds and runs clean.

### What Works

#### Core (67 files)
- [x] Arch ECS integration — entity CRUD, hierarchy, parent/child with circular-parenting prevention
- [x] 24 component types: transforms (5), hierarchy (2 + EntityRef), metadata (2), tags (3 — SelectedTag, EditorOnlyTag, EntityTag), rendering (4), physics (3), audio (1), particles (1), material (1), UI (1)
- [x] 11 systems: TransformPropagation, SpriteRendering, Animation, GumUI, Camera, Particle, SceneManager, ScreenTransition, Timer, Tween, TilemapRendering
- [x] Scene serialization — two-pass JSON with GUID-based parent linking
- [x] Component registry — typed descriptors, category filtering, no reflection
- [x] Dynamic component support — ExternalComponentLoader + DynamicComponentDescriptor for user-defined components
- [x] Sprite sheet data format (`.spritesheet.json`) + animation data format (`.animation.json`)
- [x] Texture cache, audio cache, effect cache — asset loading with path-based caching
- [x] Logging (circular buffer, 1024 max), project file I/O
- [x] Physics — PhysicsSystem, PhysicsWorld2D, CollisionLayerSettings, TileCollisionGenerator
- [x] Particles — ParticleEmitterRuntime, ParticlePool, ParticleData
- [x] Post-processing — PostProcessorPipeline, PostProcessStackData
- [x] Tilemap data — TilemapData, AutoTileRules, TiledImporter
- [x] Tween system — TweenSystem + Easing functions
- [x] Scene management — SceneManager (load/unload/transition)
- [x] Screen transitions — ScreenTransitionSystem (fade, slide, etc.)
- [x] Timer system — TimerSystem for delayed/repeating actions
- [x] Materials — MaterialData + MaterialComponent
- [x] Build profiles — BuildProfileData (Debug/Release, platform targets)
- [x] Render layers — RenderLayerConfig for sort layer configuration
- [x] Atlas data format — AtlasData + AtlasPacker
- [x] Texture import settings — TextureImportSettings

#### Editor (66 files)
- [x] 17 ImGui panels: Menu, Toolbar, Hierarchy, Inspector, Viewport, Console, AssetBrowser, SpriteSheet, Animation, StartScreen, Settings, GameRun, ParticleEditor, PostProcess, ShaderPreview, CollisionMatrix, TilemapEditor (+ About dialog in Menu)
- [x] Viewport rendering — camera pan/zoom, grid, entity markers
- [x] Gizmo tools — Move, Rotate, Scale with hit detection and drag
- [x] Selection — click, box select, Ctrl multi-select
- [x] Undo/Redo — 13 command types (Move, MoveMultiple, Create, Delete, Duplicate, Rename, Transform, ModifyComponent\<T\>, AddComponent, RemoveComponent, PaintTile, PaintTiles, FillTiles), 100-deep stack
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
- [x] Docking layout with profile save/load (LayoutProfileManager)
- [x] DPI-aware font loading (Inter, JetBrains Mono, FontAwesome)
- [x] Editor preferences management (EditorPreferences)
- [x] File dialog abstraction — IFileDialogService, MacFileDialogService, FallbackFileDialogService
- [x] Tilemap editor — palette, paint/erase/fill tools, viewport painting (TilemapEditorPanel + TilemapEditorTool + TilemapPaintHandler)
- [x] Particle editor — emitter properties, preview (ParticleEditorPanel)
- [x] Shader/effect preview panel (ShaderPreviewPanel + EffectCache)
- [x] Post-processing stack configuration (PostProcessPanel)
- [x] Collision layer matrix editor (CollisionMatrixPanel)
- [x] Audio visualization in viewport (AudioVisualization)
- [x] Physics debug overlay — contacts, raycasts (PhysicsDebugOverlay)
- [x] Game runtime bridge — build & run game from editor, console piping (GameRunPanel + GameProcessManager)
- [x] Hot reload — detect recompile, reload assemblies (HotReloadWatcher)

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
- [x] Atlas/texture packing (AtlasPacker + AtlasData)
- [x] Sprite import settings (TextureImportSettings — filter mode, wrap mode)
- [ ] Sprite origin/pivot editor in sprite sheet panel

### v0.3 — Scene Editing QoL
> Sources: G1 (Custom Code Recipes), G29 (Game Editor)

- [x] Multi-select transform (move/rotate/scale group)
- [x] Copy/paste entities (Ctrl+C/V with hierarchy)
- [x] Duplicate shortcut (Ctrl+D)
- [x] Prefab system — save entity hierarchy as reusable `.prefab.json`
- [x] Scene search/filter in hierarchy panel
- [x] Entity tags and layers

### v0.4 — Custom Code Integration
> Sources: G1 (Custom Code Recipes), G15 (Game Loop), G18 (Game Programming Patterns)

- [x] Scene manager — load/unload/transition between scenes (SceneManager)
- [x] Render layers / sort layers configurable in editor (RenderLayerConfig)
- [x] Tween system integration (TweenSystem + Easing)
- [x] Screen transitions — fade, slide, etc. (ScreenTransitionSystem)
- [x] Timer / coroutine system (TimerSystem)
- [x] Scriptable component support — user-defined components from game project (ExternalComponentLoader + DynamicComponentDescriptor)

### v0.5 — Physics & Collision Visualization
> Sources: G3 (Physics & Collision)

- [x] Collider outlines drawn in viewport (BoxCollider, CircleCollider)
- [ ] Collider editing gizmos (drag handles to resize)
- [x] Physics system (PhysicsSystem + PhysicsWorld2D)
- [x] Collision layer matrix editor in project settings (CollisionMatrixPanel + CollisionLayerSettings)
- [x] Physics debug overlay — contacts, raycasts (PhysicsDebugOverlay)

### v0.6 — Tilemap Editing
> Sources: G28 (Top-Down Perspective), G8 (Content Pipeline)

- [x] Tilemap editor panel — tileset palette, paint/erase/fill tools (TilemapEditorPanel + TilemapEditorTool)
- [x] Paint tiles directly in viewport (TilemapPaintHandler)
- [x] Auto-tiling rules — bitmask-based (AutoTileRules)
- [x] Tiled `.tmx` import (TiledImporter)
- [x] Tile collision shapes (TileCollisionGenerator)
- [x] Tilemap data + rendering (TilemapData + TilemapRenderingSystem)

### v0.7 — Camera & Display
> Sources: G19 (Display & Resolution), G20 (Camera Systems), G21 (Coordinate Systems), G24 (Window Management)

- [x] Virtual resolution preview in viewport (letterbox/pillarbox visualization)
- [x] Camera system (CameraSystem — follow target, deadzone, lookahead)
- [ ] Multi-camera support
- [ ] Viewport safe area overlay (G25)
- [ ] Resolution preset selector

### v0.8 — Particle System
> Sources: G23 (Particles)

- [x] Particle editor panel — emitter properties, curves, preview (ParticleEditorPanel)
- [x] Particle emitter component (ParticleEmitter)
- [x] Particle system runtime — emission, pooling (ParticleSystem + ParticleEmitterRuntime + ParticlePool)
- [x] Particle data format (ParticleData)
- [ ] Save/load particle presets

### v0.9 — Audio & Shaders
> Sources: G6 (Audio), G27 (Shaders & Visual Effects)

- [x] Audio cache + visualization (AudioCache + AudioVisualization)
- [x] Shader/effect preview panel (ShaderPreviewPanel + EffectCache)
- [x] Material system — shader + parameter assignment (MaterialData + MaterialComponent)
- [x] Post-processing stack configuration (PostProcessPanel + PostProcessorPipeline + PostProcessStackData)
- [ ] Audio clip playback preview in asset browser (play/stop)

### v1.0 — Game Runtime Bridge
> Sources: G29 (Game Editor), E4 (Project Management)

- [x] Build & run game project from editor (GameRunPanel + GameProcessManager)
- [x] Hot reload — detect recompile, reload assemblies (HotReloadWatcher)
- [x] Build profiles — Debug/Release, platform targets (BuildProfileData)
- [ ] Game viewport (play in editor window)
- [ ] Console output piping (game stdout → editor console)

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

(None — previous TODOs completed)

---

## Decision Log

Newest first.

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-25 | Docs audit: STATUS.md + CLAUDE.md updated to reflect actual codebase (67 Core, 66 Editor files) | Codebase grew far beyond v0.1 docs. Roadmap v0.4–v0.9 largely implemented. Updated all counts, file maps, and roadmap checkboxes. |
| 2026-02-25 | Feature push: prefabs, copy/paste, collider viz, multi-select, search, tags, drag-drop, virtual resolution, About dialog | Batch of QoL features pulled forward from v0.2–v0.7 roadmap. Completes v0.1 and partially fulfills v0.2, v0.3, v0.5, v0.7. |
| 2026-02-25 | Created STATUS.md as single-file progress tracker | Solo dev — markdown beats ticket management. Sections can migrate to GitHub Issues later. |
| 2026-02-24 | Abandoned native SwiftUI/NativeAOT rearchitecture | Tool-building trap. Deleted MonoGameStudio.Native and MonoGameStudio.macOS. ImGui is industry-proven for internal tools (Blizzard, Rockstar, id Software, Valve). Focus on making games. |
| 2026-02-24 | Chose Hexa.NET.ImGui over ImGui.NET | Active development, better .NET 10 support, native docking/tables. Required migration from ImGui.NET API patterns. |
| 2026-02-24 | Arch ECS over custom ECS | Arch 2.1.0 is fast, well-maintained, C#-native. Custom EntityRef wrapper handles safe entity storage. |
