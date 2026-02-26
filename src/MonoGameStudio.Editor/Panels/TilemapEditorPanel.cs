using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.Commands;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

/// <summary>
/// Tilemap editor panel with tileset palette, tool buttons, and layer management.
/// Works alongside TilemapPaintHandler for viewport interaction.
/// </summary>
public class TilemapEditorPanel
{
    private TilemapDocument? _document;
    private string? _currentPath;

    // Tool state
    private readonly ITilemapTool[] _tools;
    private int _selectedToolIndex;

    // Tileset palette
    private int _selectedTilesetIndex;
    private int _selectedTileId = -1;

    // Layer state
    private int _selectedLayerIndex;

    // Palette zoom
    private float _paletteZoom = 2f;

    // ImGui texture bindings for tileset preview
    private readonly Dictionary<string, nint> _tilesetTextureBindings = new();

    public TilemapDocument? Document => _document;
    public string? CurrentPath => _currentPath;
    public int SelectedTileId => _selectedTileId;
    public int SelectedLayerIndex => _selectedLayerIndex;
    public ITilemapTool CurrentTool => _tools[_selectedToolIndex];

    public TilemapEditorPanel()
    {
        _tools =
        [
            new PaintTool(),
            new EraseTool(),
            new FillTool(),
            new RectTool()
        ];
    }

    public void OpenTilemap(string path)
    {
        _document = TilemapSerializer.Load(path);
        _currentPath = path;
        _selectedLayerIndex = 0;
        _selectedTileId = -1;
        _selectedTilesetIndex = 0;
        _tilesetTextureBindings.Clear();
        Log.Info($"Opened tilemap: {path}");
    }

    public void SetDocument(TilemapDocument doc, string path)
    {
        _document = doc;
        _currentPath = path;
        _selectedLayerIndex = 0;
        _selectedTileId = -1;
        _selectedTilesetIndex = 0;
        _tilesetTextureBindings.Clear();
    }

    public void CreateNew(string savePath, int mapWidth = 32, int mapHeight = 32,
        int tileWidth = 16, int tileHeight = 16)
    {
        _document = new TilemapDocument
        {
            MapWidth = mapWidth,
            MapHeight = mapHeight,
            TileWidth = tileWidth,
            TileHeight = tileHeight
        };

        var layer = new TilemapLayer { Name = "Ground" };
        layer.Initialize(mapWidth, mapHeight);
        _document.Layers.Add(layer);

        _currentPath = savePath;
        _selectedLayerIndex = 0;
        _selectedTileId = -1;
        _selectedTilesetIndex = 0;
        Log.Info($"Created new tilemap: {mapWidth}x{mapHeight}");
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;

        if (ImGui.Begin("Tilemap Editor", ref isOpen))
        {
            if (_document == null)
            {
                ImGui.TextDisabled("No tilemap open.");
                ImGui.TextDisabled("Create or open a .tilemap.json file.");
            }
            else
            {
                DrawToolbar();
                ImGui.Separator();

                // Split: left = palette, right = layers
                float availWidth = ImGui.GetContentRegionAvail().X;
                float paletteWidth = Math.Max(200f, availWidth * 0.6f);

                if (ImGui.BeginChild("##palette_area", new Vector2(paletteWidth, 0), ImGuiChildFlags.None))
                {
                    DrawTilesetSelector();
                    DrawTilesetPalette();
                }
                ImGui.EndChild();

                ImGui.SameLine();

                if (ImGui.BeginChild("##layers_area", new Vector2(0, 0), ImGuiChildFlags.None))
                {
                    DrawLayerManager();
                    ImGui.Separator();
                    DrawMapProperties();
                }
                ImGui.EndChild();
            }
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        // Tool buttons
        string[] toolIcons = [FontAwesomeIcons.Pen, FontAwesomeIcons.Eraser, FontAwesomeIcons.FillDrip, FontAwesomeIcons.VectorSquare];
        string[] toolTips = ["Paint (B)", "Erase (E)", "Fill (G)", "Rectangle (R)"];

        for (int i = 0; i < _tools.Length; i++)
        {
            bool selected = _selectedToolIndex == i;
            if (selected)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

            if (ImGui.Button($"{toolIcons[i]}##{_tools[i].Name}"))
                _selectedToolIndex = i;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(toolTips[i]);

            if (selected)
                ImGui.PopStyleColor();

            ImGui.SameLine();
        }

        // Spacer
        ImGui.SameLine();
        float rightEdge = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rightEdge - 120);

        // Save button
        if (ImGui.Button($"{FontAwesomeIcons.Save} Save"))
            Save();

        ImGui.SameLine();

        // Palette zoom
        ImGui.SetNextItemWidth(60);
        ImGui.DragFloat("##zoom", ref _paletteZoom, 0.1f, 1f, 8f, "%.1fx");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Palette Zoom");
    }

    private void DrawTilesetSelector()
    {
        if (_document == null || _document.Tilesets.Count == 0)
        {
            ImGui.TextDisabled("No tilesets. Add one in Map Properties.");
            return;
        }

        ImGui.Text("Tileset");

        // Tileset combo
        var currentTs = _document.Tilesets[_selectedTilesetIndex];
        if (ImGui.BeginCombo("##tileset", currentTs.Name))
        {
            for (int i = 0; i < _document.Tilesets.Count; i++)
            {
                bool isSelected = _selectedTilesetIndex == i;
                if (ImGui.Selectable(_document.Tilesets[i].Name, isSelected))
                {
                    _selectedTilesetIndex = i;
                    _selectedTileId = -1;
                }
                if (isSelected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
    }

    private void DrawTilesetPalette()
    {
        if (_document == null || _document.Tilesets.Count == 0) return;
        if (_selectedTilesetIndex < 0 || _selectedTilesetIndex >= _document.Tilesets.Count) return;

        var tileset = _document.Tilesets[_selectedTilesetIndex];
        if (tileset.Columns <= 0 || tileset.TileCount <= 0)
        {
            ImGui.TextDisabled("Tileset has no tiles configured.");
            return;
        }

        int rows = (tileset.TileCount + tileset.Columns - 1) / tileset.Columns;
        float scaledTileW = tileset.TileWidth * _paletteZoom;
        float scaledTileH = tileset.TileHeight * _paletteZoom;

        ImGui.Text("Tiles");

        var startPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        float gridWidth = tileset.Columns * scaledTileW;
        float gridHeight = rows * scaledTileH;

        // Background
        drawList.AddRectFilled(startPos, startPos + new Vector2(gridWidth, gridHeight),
            ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1f)));

        // Draw tile grid
        for (int tileIdx = 0; tileIdx < tileset.TileCount; tileIdx++)
        {
            int col = tileIdx % tileset.Columns;
            int row = tileIdx / tileset.Columns;

            var tileMin = startPos + new Vector2(col * scaledTileW, row * scaledTileH);
            var tileMax = tileMin + new Vector2(scaledTileW, scaledTileH);

            // Grid cell background
            drawList.AddRect(tileMin, tileMax,
                ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)));

            // Tile index label
            drawList.AddText(tileMin + new Vector2(2, 2),
                ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 0.8f)),
                $"{tileIdx}");

            // Highlight selected tile
            int globalId = tileset.FirstGid + tileIdx;
            if (globalId == _selectedTileId)
            {
                drawList.AddRect(tileMin, tileMax,
                    ImGui.GetColorU32(new Vector4(1f, 0.8f, 0.2f, 1f)), 0f, ImDrawFlags.None, 2f);
            }
        }

        // Invisible button for click detection
        ImGui.SetCursorScreenPos(startPos);
        ImGui.InvisibleButton("##palette_grid", new Vector2(gridWidth, gridHeight));

        if (ImGui.IsItemClicked())
        {
            var mousePos = ImGui.GetMousePos() - startPos;
            int clickCol = (int)(mousePos.X / scaledTileW);
            int clickRow = (int)(mousePos.Y / scaledTileH);

            if (clickCol >= 0 && clickCol < tileset.Columns && clickRow >= 0 && clickRow < rows)
            {
                int clickedIdx = clickRow * tileset.Columns + clickCol;
                if (clickedIdx < tileset.TileCount)
                {
                    _selectedTileId = tileset.FirstGid + clickedIdx;
                }
            }
        }

        // Show selected tile info
        if (_selectedTileId >= 0)
        {
            ImGui.Text($"Selected: Tile {_selectedTileId}");
        }
    }

    private void DrawLayerManager()
    {
        if (_document == null) return;

        ImGui.Text("Layers");

        // Add/remove buttons
        if (ImGui.SmallButton($"{FontAwesomeIcons.Plus}##add_layer"))
        {
            var layer = new TilemapLayer { Name = $"Layer {_document.Layers.Count}" };
            layer.Initialize(_document.MapWidth, _document.MapHeight);
            _document.Layers.Add(layer);
        }
        ImGui.SameLine();
        if (ImGui.SmallButton($"{FontAwesomeIcons.Trash}##del_layer") && _document.Layers.Count > 1)
        {
            _document.Layers.RemoveAt(_selectedLayerIndex);
            _selectedLayerIndex = Math.Min(_selectedLayerIndex, _document.Layers.Count - 1);
        }
        ImGui.SameLine();

        // Move up
        bool canMoveUp = _selectedLayerIndex > 0;
        if (!canMoveUp) ImGui.BeginDisabled();
        if (ImGui.SmallButton("\u25b2##up"))
        {
            (_document.Layers[_selectedLayerIndex], _document.Layers[_selectedLayerIndex - 1]) =
                (_document.Layers[_selectedLayerIndex - 1], _document.Layers[_selectedLayerIndex]);
            _selectedLayerIndex--;
        }
        if (!canMoveUp) ImGui.EndDisabled();
        ImGui.SameLine();

        // Move down
        bool canMoveDown = _selectedLayerIndex < _document.Layers.Count - 1;
        if (!canMoveDown) ImGui.BeginDisabled();
        if (ImGui.SmallButton("\u25bc##down"))
        {
            (_document.Layers[_selectedLayerIndex], _document.Layers[_selectedLayerIndex + 1]) =
                (_document.Layers[_selectedLayerIndex + 1], _document.Layers[_selectedLayerIndex]);
            _selectedLayerIndex++;
        }
        if (!canMoveDown) ImGui.EndDisabled();

        // Layer list
        for (int i = _document.Layers.Count - 1; i >= 0; i--)
        {
            var layer = _document.Layers[i];
            ImGui.PushID(i);

            // Visibility toggle
            string visIcon = layer.Visible ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
            if (ImGui.SmallButton($"{visIcon}##vis"))
                layer.Visible = !layer.Visible;

            ImGui.SameLine();

            // Selectable layer name
            bool selected = _selectedLayerIndex == i;
            if (ImGui.Selectable(layer.Name, selected))
                _selectedLayerIndex = i;

            // Inline rename on double-click
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                ImGui.OpenPopup("##rename_layer");

            if (ImGui.BeginPopup("##rename_layer"))
            {
                var name = layer.Name;
                if (ImGui.InputText("##name", ref name, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    layer.Name = name;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }
    }

    private void DrawMapProperties()
    {
        if (_document == null) return;

        if (ImGui.CollapsingHeader("Map Properties"))
        {
            int mapW = _document.MapWidth;
            int mapH = _document.MapHeight;
            int tileW = _document.TileWidth;
            int tileH = _document.TileHeight;

            ImGui.InputInt("Map Width", ref mapW);
            ImGui.InputInt("Map Height", ref mapH);
            ImGui.InputInt("Tile Width", ref tileW);
            ImGui.InputInt("Tile Height", ref tileH);

            // Note: resizing is destructive, so only update on explicit action
            if (mapW != _document.MapWidth || mapH != _document.MapHeight)
            {
                if (ImGui.Button("Resize Map"))
                {
                    ResizeMap(mapW, mapH);
                }
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "Warning: destructive!");
            }
            else
            {
                _document.TileWidth = tileW;
                _document.TileHeight = tileH;
            }
        }
    }

    private void ResizeMap(int newWidth, int newHeight)
    {
        if (_document == null) return;
        if (newWidth <= 0 || newHeight <= 0) return;

        int oldWidth = _document.MapWidth;
        int oldHeight = _document.MapHeight;

        foreach (var layer in _document.Layers)
        {
            var newTiles = new int[newWidth * newHeight];
            Array.Fill(newTiles, -1);

            int copyW = Math.Min(oldWidth, newWidth);
            int copyH = Math.Min(oldHeight, newHeight);

            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    newTiles[y * newWidth + x] = layer.Tiles[y * oldWidth + x];
                }
            }

            layer.Tiles = newTiles;
        }

        _document.MapWidth = newWidth;
        _document.MapHeight = newHeight;
        Log.Info($"Resized tilemap to {newWidth}x{newHeight}");
    }

    public void Save()
    {
        if (_document == null || _currentPath == null) return;
        TilemapSerializer.Save(_currentPath, _document);
        Log.Info($"Tilemap saved: {_currentPath}");
    }

    /// <summary>
    /// Selects a tool by index: 0=Paint, 1=Erase, 2=Fill, 3=Rect.
    /// </summary>
    public void SelectTool(int index)
    {
        if (index >= 0 && index < _tools.Length)
            _selectedToolIndex = index;
    }
}

// FontAwesome icons used by this panel that may not exist in the shared icons file.
// These are defined locally to avoid modifying the shared constants.
file static class FontAwesomeIcons
{
    public const string Pen = "\uf304";
    public const string Eraser = "\uf12d";
    public const string FillDrip = "\uf576";
    public const string VectorSquare = "\uf5cb";
    public const string Save = "\uf0c7";
    public const string Plus = "\uf067";
    public const string Trash = "\uf1f8";
    public const string Eye = "\uf06e";
    public const string EyeSlash = "\uf070";
}
