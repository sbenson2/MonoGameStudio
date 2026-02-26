# MonoGameStudio

A 2D game editor built with MonoGame + Arch ECS + ImGui. Visual development tool for MonoGame games.

## Architecture

**MonoGame Game + ImGui UI**: Single MonoGame `Game` window with ImGui rendering all editor panels via dockspace. Core engine library has zero editor dependencies.

### Project Structure

```
src/
├── MonoGameStudio.Core/      # ECS components, systems, serialization, logging (ZERO editor deps)
├── MonoGameStudio.Editor/    # ImGui panels, gizmos, viewport, commands
└── MonoGameStudio.Desktop/   # Thin launcher
```

**Core must never reference Editor.** Editor references Core. Desktop references Editor.

## Build & Run

```bash
dotnet run --project src/MonoGameStudio.Desktop
```

## Tech Stack

- .NET 10 (C# 13), `AllowUnsafeBlocks`, `Nullable`, `ImplicitUsings` all enabled
- MonoGame.Framework.DesktopGL 3.8.x
- Arch ECS 2.1.0
- Hexa.NET.ImGui 2.2.9 (migrated from ImGui.NET)
- ktsu.ImGuiStyler 1.3.12 (Catppuccin.Mocha theme + theme selector)
- MonoGameGum (Gum.MonoGame 2026.2.*) for in-game UI
- System.Text.Json for scene serialization

### Fonts
- Inter (UI font, 15px base)
- JetBrains Mono (console font, 14px base)
- FontAwesome 6 Free Solid (icon font, merged into UI font)
- All in `src/MonoGameStudio.Editor/Content/Fonts/`

## File Map

### Core (28+ files)
| File | Purpose |
|------|---------|
| `Components/Position.cs` | `struct Position { float X, Y }` |
| `Components/Rotation.cs` | `struct Rotation { float Angle }` (radians) |
| `Components/Scale.cs` | `struct Scale { float X, Y }` |
| `Components/LocalTransform.cs` | `struct LocalTransform { Matrix WorldMatrix }` (computed) |
| `Components/WorldTransform.cs` | `struct WorldTransform { Matrix WorldMatrix }` (computed with parent) |
| `Components/Parent.cs` | `struct Parent { EntityRef Ref }` |
| `Components/Children.cs` | `struct Children { List<EntityRef> Refs }` |
| `Components/EntityName.cs` | `struct EntityName { string Name }` |
| `Components/EntityGuid.cs` | `struct EntityGuid { Guid Id }` |
| `Components/Tags.cs` | `SelectedTag`, `EditorOnlyTag` (empty structs) |
| `Components/GumScreen.cs` | `struct GumScreen { GumProjectPath, ScreenName, IsActive }` |
| `Components/ComponentCategory.cs` | `[ComponentCategory("...")]` attribute for Add Component picker |
| `Components/Rendering.cs` | SpriteRenderer, Animator, Camera2D, TilemapRenderer |
| `Components/Physics.cs` | BoxCollider, CircleCollider, RigidBody2D |
| `Components/Audio.cs` | AudioSource |
| `World/WorldManager.cs` | Arch world wrapper — entity CRUD, hierarchy, reparenting |
| `Systems/TransformPropagationSystem.cs` | Recursive Local→World transform each frame |
| `Systems/GumUISystem.cs` | Loads/activates Gum screens from GumScreen components |
| `UI/GumUIManager.cs` | MonoGameGum.GumService wrapper |
| `Serialization/SceneSerializer.cs` | Generic JSON save/load via ComponentDescriptor (no reflection) |
| `Serialization/ComponentRegistry.cs` | Component type registry with categories, addable/serializable type filtering |
| `Serialization/ComponentDescriptor.cs` | Generic descriptor — typed Has/Get/Set/Add/Remove without reflection |
| `Serialization/FieldDescriptor.cs` | Field metadata + typed get/set delegates |
| `Serialization/SceneData.cs` | DTOs for scene JSON |
| `Logging/Log.cs` | Static circular buffer (1024 max), `OnLog` event |
| `Data/EditorMode.cs` | `enum EditorMode { Edit, Play, Pause }` |
| `Data/ApplicationPhase.cs` | `enum ApplicationPhase { StartScreen, Editor }` |
| `Project/ProjectInfo.cs` | Project metadata DTO matching `.mgstudio` JSON format |
| `Project/RecentProject.cs` | Recent project entry + list DTOs |
| `Project/ProjectSerializer.cs` | Read/write `.mgstudio` JSON files |
| `Assets/TextureCache.cs` | Raw texture loading via `Texture2D.FromStream`, cache by path |
| `Data/SpriteSheetData.cs` | SpriteSheetDocument, SpriteFrame, SpriteSheetSerializer |
| `Data/AnimationData.cs` | AnimationDocument, AnimationClip, AnimationFrameRef, AnimationSerializer |
| `Systems/SpriteRenderingSystem.cs` | Queries SpriteRenderer + Position, sorts by SortOrder, draws via SpriteBatch |
| `Systems/AnimationSystem.cs` | Queries Animator + SpriteRenderer, advances frame timer, updates SourceRect |

### Editor (43 files)
| File | Purpose |
|------|---------|
| `Editor/EditorGame.cs` | **Main Game class** — init, update loop, draw loop, wires everything |
| `Editor/EditorState.cs` | Mode, selection, panel visibility, scene path, dirty flag |
| `Editor/PlayModeManager.cs` | Scene snapshot/restore for play mode |
| `Editor/ShortcutManager.cs` | Keyboard shortcuts (Ctrl+S/O/N, Ctrl+Z, Q/W/E/R, etc.) |
| `Commands/ICommand.cs` | `interface ICommand { Execute(), Undo(), Description }` |
| `Commands/CommandHistory.cs` | Undo/redo stacks (max 100) |
| `Commands/EntityCommands.cs` | Move, Create, Delete, Rename, ModifyComponent commands |
| `Commands/ComponentCommands.cs` | AddComponent, RemoveComponent commands + ComponentReflection helpers |
| `ImGui/ImGuiManager.cs` | ImGui context, fonts, theming, DPI |
| `ImGui/ImGuiRenderer.cs` | Hexa.NET.ImGui render backend for MonoGame/OpenGL |
| `ImGui/DrawVertDeclaration.cs` | Vertex format for ImGui rendering |
| `ImGui/FontAwesomeIcons.cs` | Icon codepoint constants |
| `Layout/DockingLayout.cs` | ImGui dockspace setup |
| `Layout/LayoutDefinitions.cs` | Default panel positions |
| `Panels/MenuBarPanel.cs` | File/Edit/View/Help menus (+ macOS native) |
| `Panels/ToolbarPanel.cs` | Gizmo mode + Play/Pause/Stop buttons |
| `Panels/SceneHierarchyPanel.cs` | Entity tree, drag-drop reparenting, context menu |
| `Panels/InspectorPanel.cs` | Reflection-based component editing |
| `Panels/GameViewportPanel.cs` | Renders scene RT (OpenGL Y-flip) |
| `Panels/ConsolePanel.cs` | Log display with filters, search, auto-scroll |
| `Assets/AssetEntry.cs` | Asset DTO + type classification (Texture, Audio, Scene, etc.) |
| `Assets/AssetDatabase.cs` | File system scanning, caching, search, FileSystemWatcher |
| `Panels/AssetBrowserPanel.cs` | Two-pane file browser: folder tree, grid/list view, search, thumbnails, drag-drop |
| `Panels/SpriteSheetPanel.cs` | Texture display, grid overlay, auto-slice, frame editing, save as `.spritesheet.json` |
| `Panels/AnimationPanel.cs` | Timeline via ImDrawList, clip tabs, playback preview, frame editing, save as `.animation.json` |
| `Panels/StartScreenPanel.cs` | Full-viewport start screen: new project wizard, recent projects |
| `Project/ProjectManager.cs` | Project lifecycle: create, open, close, save settings |
| `Project/ProjectTemplate.cs` | Template definitions (Empty, 2D Platformer, Top-Down RPG) |
| `Project/ProjectScaffolder.cs` | Directory/file creation + dotnet new scaffolding |
| `Project/UserDataManager.cs` | Recent projects persistence in OS user data dir |
| `Inspector/ComponentDrawer.cs` | Reflects on struct fields, draws ImGui controls |
| `Inspector/FieldDrawers.cs` | Type→widget dispatch (float, int, Vector2, Color, Rectangle, etc.) |
| `Inspector/ComponentPicker.cs` | Searchable Add Component popup grouped by category |
| `Viewport/EditorCamera.cs` | Pan (MMB), zoom (scroll), screen↔world transforms |
| `Viewport/ViewportRenderer.cs` | RenderTarget2D management, texture binding |
| `Viewport/GridRenderer.cs` | Background grid |
| `Gizmos/GizmoManager.cs` | Move/Rotate/Scale gizmos, hit detection, drag |
| `Gizmos/GizmoRenderer.cs` | Gizmo shape drawing (arrows, circles, rects) |
| `Gizmos/SelectionSystem.cs` | Click + box select + Ctrl multi-select |
| `Platform/ObjCRuntime.cs` | macOS Objective-C interop |
| `Platform/MacMenuBar.cs` | Native macOS menu bar |
| `Platform/MacMenuCallbacks.cs` | macOS menu event handlers |
| `Platform/MacTitleBar.cs` | Custom title bar + traffic light positioning |

### Desktop (1 file)
| File | Purpose |
|------|---------|
| `Program.cs` | `new EditorGame(args).Run()` — passes CLI args for project opening |

## Coding Conventions

- **Namespaces**: `MonoGameStudio.[Project].[Feature]` (e.g., `MonoGameStudio.Core.Components`)
- **Components**: Pure structs — no logic, no references to managers
- **Managers/Panels**: Classes with clear single responsibility
- **Entity safety**: Always call `_world.IsAlive(entity)` before accessing entity data
- **No component mutation during queries**: Don't add/remove components inside Arch query iteration
- **Undo/Redo**: All editor mutations that affect scene state must go through `ICommand` + `CommandHistory`

## Key Patterns

- **Input routing priority** (Edit mode): ImGui capture > Gizmo interaction > Selection > Camera
- **Transform hierarchy**: `TransformPropagationSystem` recursively computes `WorldTransform = LocalTransform * ParentWorldTransform` each frame
- **Scene serialization**: Two-pass JSON — create all entities first, then link parent references by GUID
- **Component descriptors**: `ComponentDescriptor<T>` provides typed Has/Get/Set/Add/Remove without reflection
- **Logging**: Static circular buffer (`Log.cs`, max 1024 entries) with `OnLog` event
- **Play mode**: Serialize scene → snapshot JSON, on Stop → deserialize snapshot back

## Current State

### Working
- Full ECS pipeline (entity CRUD, hierarchy, transform propagation)
- All editor panels (hierarchy, inspector, console, viewport, toolbar, menu)
- Viewport rendering with camera pan/zoom, grid, entity markers
- Gizmo tools (Move, Rotate, Scale) with hit detection
- Selection system (click, box select, Ctrl multi-select)
- Undo/Redo (Move, Create, Delete, Rename, ModifyComponent)
- Scene serialization (JSON, GUID-based parent linking)
- Gum UI integration (GumScreen component + manager)
- Play mode (snapshot/restore)
- Keyboard shortcuts
- macOS native menu bar + title bar
- Start screen with new project wizard, recent projects, templates
- Project management (.mgstudio files, create/open/close projects)
- `dotnet new mgdesktopgl` scaffolding for new MonoGame projects
- CLI project opening (`dotnet run -- /path/to/project.mgstudio`)
- Camera settings persisted per-project
- Add Component picker with search and category grouping
- Remove Component via right-click context menu (with undo/redo)
- 8 built-in game components: SpriteRenderer, Animator, Camera2D, TilemapRenderer, BoxCollider, CircleCollider, RigidBody2D, AudioSource
- Generic scene serialization (any registered component auto-serializes)
- Rectangle field drawer in inspector
- Full asset browser: folder tree, grid/list view, search, type filter tabs
- Texture thumbnails loaded via TextureCache → ImGui texture binding
- Drag-drop source on assets (ASSET_PATH payload)
- FileSystemWatcher for auto-refresh on external changes
- Show in Finder context menu
- Breadcrumb navigation
- Sprite rendering system (SpriteRenderer component → SpriteBatch draw, sorted by SortOrder)
- Animation system (Animator component advances frames, updates SpriteRenderer.SourceRect)
- Sprite sheet editor panel (texture preview, auto-slice by cols/rows, frame rect editing, zoom)
- Animation panel (clip tabs, horizontal timeline, playback preview, frame properties)
- `.spritesheet.json` and `.animation.json` file formats with serializers

### Incomplete
- **About dialog** — TODO in MenuBarPanel

## Known Quirks

- `ImGuiIOPtr.IniFilename` is read-only — set via `io.Handle->IniFilename` with a stable native string
- `DockBuilder*` functions use `ImGuiP.DockBuilder*()` (Hexa.NET.ImGui exposes them natively)
- `ImGuiP.DockBuilderSplitNode()` takes `uint*` pointers — use `unsafe` block
- OpenGL Y-flip: pass `uv0=(0,1), uv1=(1,0)` for RenderTarget2D in `ImGui.Image()`
- Hexa.NET.ImGui texture protocol: backends set `RendererHasTextures` flag and process `ImTextureStatus.WantCreate/WantUpdates/WantDestroy` each frame
- `ImTextureRef` replaces `IntPtr` for texture IDs — construct with `new ImTextureRef(null, texId)`
- `ImDrawCmd.TexRef` replaces `ImDrawCmd.TextureId` — use `drawCmd->TexRef.GetTexID()` to resolve
- `ImGuiKey._0` is now `ImGuiKey.Key0` in Hexa.NET.ImGui
- `ImFontConfig` fields `MergeMode`/`PixelSnapH` are `byte` not `bool` — use `1`/`0`
- `PushFont(font, size)` requires a size parameter (not just font)
- Arch 2.x: `EntityReference` doesn't exist — use custom `EntityRef` struct
- `Viewport` namespace conflicts with `Microsoft.Xna.Framework.Graphics.Viewport` — fully qualify in `ImGuiRenderer.cs`
- `MenuItem(label, null, ref bool)` is ambiguous — cast to `(string?)null` to resolve
