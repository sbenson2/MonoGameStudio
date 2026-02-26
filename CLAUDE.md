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

### Core (67 files)

#### Components (12 files, 24 component types)
| File | Purpose |
|------|---------|
| `Components/Transform.cs` | Position, Rotation, Scale, LocalTransform, WorldTransform (5 structs) |
| `Components/Hierarchy.cs` | EntityRef, Parent, Children |
| `Components/EntityMetadata.cs` | EntityName, EntityGuid |
| `Components/Tags.cs` | SelectedTag, EditorOnlyTag, EntityTag |
| `Components/Rendering.cs` | SpriteRenderer, Animator, Camera2D, TilemapRenderer |
| `Components/Physics.cs` | BoxCollider, CircleCollider, RigidBody2D |
| `Components/Audio.cs` | AudioSource |
| `Components/Particles.cs` | ParticleEmitter |
| `Components/Material.cs` | MaterialComponent |
| `Components/GumScreen.cs` | GumScreen (Gum UI integration) |
| `Components/ComponentCategory.cs` | `[ComponentCategory("...")]` attribute for Add Component picker |
| `Components/GameComponentAttribute.cs` | Attribute for marking game components |

#### Systems (11 files)
| File | Purpose |
|------|---------|
| `Systems/TransformPropagationSystem.cs` | Recursive Local→World transform each frame |
| `Systems/SpriteRenderingSystem.cs` | Queries SpriteRenderer + Position, sorts by SortOrder, draws via SpriteBatch |
| `Systems/AnimationSystem.cs` | Queries Animator + SpriteRenderer, advances frame timer, updates SourceRect |
| `Systems/GumUISystem.cs` | Loads/activates Gum screens from GumScreen components |
| `Systems/CameraSystem.cs` | Camera follow target, deadzone, lookahead |
| `Systems/ParticleSystem.cs` | Particle emission, update, rendering |
| `Systems/TilemapRenderingSystem.cs` | Tilemap layer rendering |
| `Systems/SceneManager.cs` | Scene load/unload/transition |
| `Systems/ScreenTransitionSystem.cs` | Screen transitions (fade, slide, etc.) |
| `Systems/TimerSystem.cs` | Delayed/repeating timer actions |
| `Systems/TweenSystem.cs` | Property tweening with easing functions |

#### Serialization (11 files)
| File | Purpose |
|------|---------|
| `Serialization/ComponentRegistry.cs` | Component type registry with categories, addable/serializable type filtering |
| `Serialization/ComponentDescriptor.cs` | Generic descriptor — typed Has/Get/Set/Add/Remove without reflection |
| `Serialization/IComponentDescriptor.cs` | Interface for component descriptors |
| `Serialization/DynamicComponentDescriptor.cs` | Runtime descriptor for user-defined components |
| `Serialization/FieldDescriptor.cs` | Field metadata + typed get/set delegates |
| `Serialization/ComponentRegistrations.cs` | Built-in component registration definitions |
| `Serialization/SceneSerializer.cs` | Generic JSON save/load via ComponentDescriptor (no reflection) |
| `Serialization/SceneData.cs` | DTOs for scene JSON |
| `Serialization/PrefabSerializer.cs` | Prefab save/load (`.prefab.json`) |
| `Serialization/ExternalComponentLoader.cs` | Loads user-defined components from game project assemblies |
| `Serialization/JsonConverters.cs` | Custom System.Text.Json converters |

#### Data (15 files)
| File | Purpose |
|------|---------|
| `Data/EditorMode.cs` | `enum EditorMode { Edit, Play, Pause }` |
| `Data/ApplicationPhase.cs` | `enum ApplicationPhase { StartScreen, Editor }` |
| `Data/SpriteSheetData.cs` | SpriteSheetDocument, SpriteFrame, SpriteSheetSerializer |
| `Data/AnimationData.cs` | AnimationDocument, AnimationClip, AnimationFrameRef, AnimationSerializer |
| `Data/TilemapData.cs` | Tilemap layer/tile data structures |
| `Data/AutoTileRules.cs` | Bitmask-based auto-tiling rules |
| `Data/TiledImporter.cs` | Tiled `.tmx` file import |
| `Data/ParticleData.cs` | Particle emitter configuration data |
| `Data/MaterialData.cs` | Material/shader parameter data |
| `Data/PostProcessStackData.cs` | Post-processing effect stack configuration |
| `Data/BuildProfileData.cs` | Build profiles (Debug/Release, platform targets) |
| `Data/RenderLayerConfig.cs` | Render/sort layer configuration |
| `Data/Easing.cs` | Easing functions for tweens |
| `Data/AtlasData.cs` | Texture atlas data format |
| `Data/TextureImportSettings.cs` | Sprite import settings (filter mode, wrap mode) |

#### Assets (4 files)
| File | Purpose |
|------|---------|
| `Assets/TextureCache.cs` | Raw texture loading via `Texture2D.FromStream`, cache by path |
| `Assets/AudioCache.cs` | Audio asset caching |
| `Assets/EffectCache.cs` | Shader/effect caching |
| `Assets/AtlasPacker.cs` | Texture atlas packing (combine loose sprites) |

#### Physics (4 files)
| File | Purpose |
|------|---------|
| `Physics/PhysicsSystem.cs` | 2D physics simulation |
| `Physics/PhysicsWorld2D.cs` | Physics world management |
| `Physics/CollisionLayerSettings.cs` | Collision layer matrix configuration |
| `Physics/TileCollisionGenerator.cs` | Generate collision shapes from tilemap data |

#### Particles (3 files)
| File | Purpose |
|------|---------|
| `Particles/Particle.cs` | Particle struct / data |
| `Particles/ParticleEmitterRuntime.cs` | Runtime particle emission logic |
| `Particles/ParticlePool.cs` | Object pool for particle instances |

#### Rendering (1 file)
| File | Purpose |
|------|---------|
| `Rendering/PostProcessorPipeline.cs` | Post-processing effect pipeline |

#### Project (3 files)
| File | Purpose |
|------|---------|
| `Project/ProjectInfo.cs` | Project metadata DTO matching `.mgstudio` JSON format |
| `Project/RecentProject.cs` | Recent project entry + list DTOs |
| `Project/ProjectSerializer.cs` | Read/write `.mgstudio` JSON files |

#### Other Core (3 files)
| File | Purpose |
|------|---------|
| `World/WorldManager.cs` | Arch world wrapper — entity CRUD, hierarchy, reparenting |
| `UI/GumUIManager.cs` | MonoGameGum.GumService wrapper |
| `Logging/Log.cs` | Static circular buffer (1024 max), `OnLog` event |

### Editor (66 files)

#### Editor Core (5 files)
| File | Purpose |
|------|---------|
| `Editor/EditorGame.cs` | **Main Game class** — init, update loop, draw loop, wires everything |
| `Editor/EditorState.cs` | Mode, selection, panel visibility, scene path, dirty flag |
| `Editor/EditorPreferences.cs` | Editor preferences management |
| `Editor/PlayModeManager.cs` | Scene snapshot/restore for play mode |
| `Editor/ShortcutManager.cs` | Keyboard shortcuts (Ctrl+S/O/N, Ctrl+Z, Q/W/E/R, etc.) |

#### Commands (6 files, 13 command types)
| File | Purpose |
|------|---------|
| `Commands/ICommand.cs` | `interface ICommand { Execute(), Undo(), Description }` |
| `Commands/CommandHistory.cs` | Undo/redo stacks (max 100) |
| `Commands/EntityCommands.cs` | Move, Create, Delete, Rename, Transform, MoveMultiple, Duplicate, ModifyComponent\<T\> |
| `Commands/ComponentCommands.cs` | AddComponent, RemoveComponent |
| `Commands/TilemapCommands.cs` | PaintTile, PaintTiles, FillTiles |
| `Commands/ClipboardManager.cs` | Copy/paste entity data (Ctrl+C/V) |

#### Panels (18 files, 17 panels)
| File | Purpose |
|------|---------|
| `Panels/MenuBarPanel.cs` | File/Edit/View/Help menus + About dialog (+ macOS native) |
| `Panels/ToolbarPanel.cs` | Gizmo mode + Play/Pause/Stop buttons |
| `Panels/SceneHierarchyPanel.cs` | Entity tree, drag-drop reparenting, context menu |
| `Panels/InspectorPanel.cs` | Component property editing |
| `Panels/GameViewportPanel.cs` | Renders scene RT (OpenGL Y-flip) |
| `Panels/ConsolePanel.cs` | Log display with filters, search, auto-scroll |
| `Panels/AssetBrowserPanel.cs` | Two-pane file browser: folder tree, grid/list view, search, thumbnails, drag-drop |
| `Panels/SpriteSheetPanel.cs` | Texture display, grid overlay, auto-slice, frame editing |
| `Panels/AnimationPanel.cs` | Timeline, clip tabs, playback preview, frame editing |
| `Panels/StartScreenPanel.cs` | Full-viewport start screen: new project wizard, recent projects |
| `Panels/SettingsPanel.cs` | Editor settings (virtual resolution, etc.) |
| `Panels/GameRunPanel.cs` | Build & run game project from editor |
| `Panels/ParticleEditorPanel.cs` | Particle emitter properties, curves, preview |
| `Panels/PostProcessPanel.cs` | Post-processing stack configuration |
| `Panels/ShaderPreviewPanel.cs` | Shader/effect preview |
| `Panels/CollisionMatrixPanel.cs` | Collision layer matrix editor |
| `Panels/TilemapEditorPanel.cs` | Tileset palette, paint/erase/fill tools |
| `Panels/TilemapEditorTool.cs` | Tilemap tool state and interaction logic |

#### ImGui (4 files)
| File | Purpose |
|------|---------|
| `ImGui/ImGuiManager.cs` | ImGui context, fonts, theming, DPI |
| `ImGui/ImGuiRenderer.cs` | Hexa.NET.ImGui render backend for MonoGame/OpenGL |
| `ImGui/DrawVertDeclaration.cs` | Vertex format for ImGui rendering |
| `ImGui/FontAwesomeIcons.cs` | Icon codepoint constants |

#### Layout (3 files)
| File | Purpose |
|------|---------|
| `Layout/DockingLayout.cs` | ImGui dockspace setup |
| `Layout/LayoutDefinitions.cs` | Default panel positions |
| `Layout/LayoutProfileManager.cs` | Docking layout profile save/load |

#### Gizmos & Visualization (6 files)
| File | Purpose |
|------|---------|
| `Gizmos/GizmoManager.cs` | Move/Rotate/Scale gizmos, hit detection, drag |
| `Gizmos/GizmoRenderer.cs` | Gizmo shape drawing (arrows, circles, rects) |
| `Gizmos/SelectionSystem.cs` | Click + box select + Ctrl multi-select |
| `Gizmos/ColliderVisualization.cs` | Wireframe outlines for BoxCollider/CircleCollider |
| `Gizmos/AudioVisualization.cs` | Audio source visualization in viewport |
| `Gizmos/PhysicsDebugOverlay.cs` | Physics contacts, raycasts debug drawing |

#### Inspector (3 files)
| File | Purpose |
|------|---------|
| `Inspector/ComponentDrawer.cs` | Reflects on struct fields, draws ImGui controls |
| `Inspector/FieldDrawers.cs` | Type→widget dispatch (float, int, Vector2, Color, Rectangle, etc.) |
| `Inspector/ComponentPicker.cs` | Searchable Add Component popup grouped by category |

#### Viewport (4 files)
| File | Purpose |
|------|---------|
| `Viewport/EditorCamera.cs` | Pan (MMB), zoom (scroll), screen↔world transforms |
| `Viewport/ViewportRenderer.cs` | RenderTarget2D management, texture binding |
| `Viewport/GridRenderer.cs` | Background grid |
| `Viewport/TilemapPaintHandler.cs` | Tile painting directly in viewport |

#### Assets (2 files)
| File | Purpose |
|------|---------|
| `Assets/AssetDatabase.cs` | File system scanning, caching, search, FileSystemWatcher |
| `Assets/AssetEntry.cs` | Asset DTO + type classification (Texture, Audio, Scene, etc.) |

#### Project (4 files)
| File | Purpose |
|------|---------|
| `Project/ProjectManager.cs` | Project lifecycle: create, open, close, save settings |
| `Project/ProjectTemplate.cs` | Template definitions (Empty, 2D Platformer, Top-Down RPG) |
| `Project/ProjectScaffolder.cs` | Directory/file creation + dotnet new scaffolding |
| `Project/UserDataManager.cs` | Recent projects persistence in OS user data dir |

#### Platform (9 files)
| File | Purpose |
|------|---------|
| `Platform/IFileDialogService.cs` | File dialog abstraction interface |
| `Platform/MacFileDialogService.cs` | macOS native file dialogs via ObjC interop |
| `Platform/FallbackFileDialogService.cs` | Fallback file dialog (non-macOS) |
| `Platform/ObjCRuntime.cs` | macOS Objective-C interop |
| `Platform/MacMenuBar.cs` | Native macOS menu bar |
| `Platform/MacMenuCallbacks.cs` | macOS menu event handlers |
| `Platform/MacTitleBar.cs` | Custom title bar + traffic light positioning |
| `Platform/MacToolbar.cs` | Native macOS toolbar |
| `Platform/MacToolbarCallbacks.cs` | macOS toolbar event handlers |

#### Runtime (2 files)
| File | Purpose |
|------|---------|
| `Runtime/GameProcessManager.cs` | Build & run game process from editor |
| `Runtime/HotReloadWatcher.cs` | File watcher for hot reload on recompile |

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
- 17 editor panels (hierarchy, inspector, console, viewport, toolbar, menu, asset browser, sprite sheet, animation, start screen, settings, game run, particle editor, post-process, shader preview, collision matrix, tilemap editor)
- Viewport rendering with camera pan/zoom, grid, entity markers
- Gizmo tools (Move, Rotate, Scale) with hit detection
- Selection system (click, box select, Ctrl multi-select)
- Undo/Redo (13 command types, 100-deep stack)
- Scene serialization (JSON, GUID-based parent linking)
- Gum UI integration (GumScreen component + manager)
- Play mode (snapshot/restore)
- Keyboard shortcuts
- macOS native menu bar + title bar + toolbar + file dialogs
- Start screen with new project wizard, recent projects, templates
- Project management (.mgstudio files, create/open/close projects)
- `dotnet new mgdesktopgl` scaffolding for new MonoGame projects
- CLI project opening (`dotnet run -- /path/to/project.mgstudio`)
- Camera system (follow target, deadzone, lookahead)
- Add Component picker with search and category grouping
- Remove Component via right-click context menu (with undo/redo)
- 24 component types across transforms, rendering, physics, audio, particles, materials, hierarchy, metadata, tags, UI
- Generic scene serialization (any registered component auto-serializes)
- Dynamic component support (user-defined components from game projects)
- Full asset browser: folder tree, grid/list view, search, type filter tabs, thumbnails, drag-drop, FileSystemWatcher
- Sprite rendering + animation systems
- Sprite sheet editor + animation panel with `.spritesheet.json` / `.animation.json` formats
- Tilemap editor — palette, paint/erase/fill, auto-tiling, Tiled .tmx import, tile collision generation
- Particle system — editor panel, emitter component, runtime + pooling
- Physics — PhysicsSystem, PhysicsWorld2D, collision layer matrix, debug overlay
- Shader/effect preview panel with effect caching
- Material system (MaterialData + MaterialComponent)
- Post-processing stack configuration (pipeline + editor panel)
- Audio cache + audio visualization in viewport
- Scene manager (load/unload/transition)
- Screen transitions (fade, slide, etc.)
- Tween system with easing functions
- Timer system for delayed/repeating actions
- Render layer configuration
- Atlas/texture packing
- Build profiles (Debug/Release, platform targets)
- Game runtime bridge — build & run from editor, hot reload
- Editor preferences management
- Docking layout with profile save/load
- Copy/paste entities, prefab system, multi-select transform, entity search/filter, entity tags
- Collider + physics debug visualization
- Virtual resolution preview
- About dialog (Help menu)

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
