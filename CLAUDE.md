# MonoGameStudio

A 2D game editor built with MonoGame + ImGui.NET + Arch ECS.

## Architecture

**Game-First with Editor Overlay**: MonoGame is the main application; ImGui.NET renders editor panels on top of the game viewport. Single render pipeline for both edit and play modes.

### 3-Project Solution

```
src/
├── MonoGameStudio.Core/      # ECS components, systems, serialization, logging (ZERO editor deps)
├── MonoGameStudio.Editor/    # ImGui panels, gizmos, viewport, commands (depends on Core)
└── MonoGameStudio.Desktop/   # Thin launcher: new EditorGame().Run()
```

**Core must never reference Editor.** This boundary is critical.

## Tech Stack

- .NET 9 (C# 13), `AllowUnsafeBlocks`, `Nullable`, `ImplicitUsings` all enabled
- MonoGame.Framework.DesktopGL 3.8.x
- Arch ECS 2.1.0
- ImGui.NET 1.91.6.1
- System.Text.Json for scene serialization

## Build & Run

```bash
dotnet run --project src/MonoGameStudio.Desktop
```

## Coding Conventions

- **Namespaces**: `MonoGameStudio.[Project].[Feature]` (e.g., `MonoGameStudio.Core.Components`)
- **Components**: Pure structs (Position, Rotation, Scale, etc.) — no logic, no references to managers
- **Managers/Panels**: Classes with clear single responsibility
- **Entity safety**: Always call `_world.IsAlive(entity)` before accessing entity data
- **No component mutation during queries**: Don't add/remove components inside Arch query iteration
- **Undo/Redo**: All editor mutations that affect scene state must go through `ICommand` + `CommandHistory`

## Key Patterns

- **Input routing priority** (Edit mode): ImGui capture > Gizmo interaction > Selection > Camera
- **Transform hierarchy**: `TransformPropagationSystem` recursively computes `WorldTransform = LocalTransform * ParentWorldTransform` each frame
- **Scene serialization**: Two-pass JSON — create all entities first, then link parent references by GUID
- **Logging**: Static circular buffer (`Log.cs`, max 1024 entries) with `OnLog` event

## Known Quirks

- `ImGuiIOPtr.IniFilename` is read-only — set via `io.NativePtr->IniFilename` with a stable native string
- `DockBuilder*` functions require P/Invoke to `cimgui` (see `ImGuiDockBuilder.cs`)
- `ImGuiDockNodeFlags.DockSpace` doesn't exist in the binding — use raw value `1 << 2`
- OpenGL Y-flip: pass `uv0=(0,1), uv1=(1,0)` for RenderTarget2D in `ImGui.Image()`
- Arch 2.x: `EntityReference` doesn't exist — use custom `EntityRef` struct
- `Viewport` namespace conflicts with `Microsoft.Xna.Framework.Graphics.Viewport` — fully qualify in `ImGuiRenderer.cs`
