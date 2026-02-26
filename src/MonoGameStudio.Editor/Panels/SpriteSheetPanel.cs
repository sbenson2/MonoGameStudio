using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class SpriteSheetPanel
{
    private TextureCache? _textureCache;
    private ImGuiManager? _imGui;

    private SpriteSheetDocument? _document;
    private string? _currentPath;
    private Texture2D? _texture;
    private ImTextureRef? _textureRef;

    // Auto-slice settings
    private int _sliceCols = 4;
    private int _sliceRows = 4;
    private int _selectedFrame = -1;

    // Zoom/pan
    private float _zoom = 1f;
    private Vector2 _scroll = Vector2.Zero;

    // Pivot drag state
    private bool _isDraggingPivot;

    public void Initialize(TextureCache textureCache, ImGuiManager imGui)
    {
        _textureCache = textureCache;
        _imGui = imGui;
    }

    public void OpenSpriteSheet(string path)
    {
        _document = SpriteSheetSerializer.Load(path);
        _currentPath = path;
        _selectedFrame = -1;

        if (_document != null && !string.IsNullOrEmpty(_document.TexturePath))
            LoadTexture(_document.TexturePath);

        Log.Info($"Opened sprite sheet: {path}");
    }

    public void CreateFromTexture(string texturePath, string savePath)
    {
        _document = new SpriteSheetDocument { TexturePath = texturePath };
        _currentPath = savePath;
        _selectedFrame = -1;
        LoadTexture(texturePath);
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.SpriteSheetEditor, ref isOpen))
        {
            if (_document == null)
            {
                ImGui.TextDisabled("No sprite sheet open.");
                ImGui.TextDisabled("Right-click a texture in the Asset Browser to create one.");
            }
            else
            {
                DrawToolbar();
                DrawContent();
            }
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        // Auto-slice controls
        ImGui.Text("Auto-Slice:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(60);
        ImGui.InputInt("Cols", ref _sliceCols);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(60);
        ImGui.InputInt("Rows", ref _sliceRows);
        ImGui.SameLine();

        _sliceCols = Math.Max(1, _sliceCols);
        _sliceRows = Math.Max(1, _sliceRows);

        if (ImGui.Button("Slice"))
        {
            AutoSlice();
        }
        ImGui.SameLine();

        if (ImGui.Button("Save"))
        {
            Save();
        }
        ImGui.SameLine();

        ImGui.Text($"Frames: {_document?.Frames.Count ?? 0}");

        ImGui.Separator();
    }

    private void DrawContent()
    {
        float availWidth = ImGui.GetContentRegionAvail().X;

        // Left: texture preview with grid overlay
        float previewWidth = availWidth * 0.65f;
        if (ImGui.BeginChild("SpritePreview", new Vector2(previewWidth, 0), ImGuiChildFlags.Borders))
        {
            DrawTexturePreview();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Right: frame list
        if (ImGui.BeginChild("FrameList", Vector2.Zero, ImGuiChildFlags.Borders))
        {
            DrawFrameList();
        }
        ImGui.EndChild();
    }

    private void DrawTexturePreview()
    {
        if (_textureRef == null || _texture == null) return;

        var cursorPos = ImGui.GetCursorScreenPos();
        var availSize = ImGui.GetContentRegionAvail();

        // Fit texture into available space
        float scaleX = availSize.X / _texture.Width;
        float scaleY = availSize.Y / _texture.Height;
        float fitScale = Math.Min(scaleX, scaleY) * _zoom;

        var imgSize = new Vector2(_texture.Width * fitScale, _texture.Height * fitScale);
        ImGui.Image(_textureRef.Value, imgSize, new Vector2(0, 0), new Vector2(1, 1));

        // Draw frame rectangles overlay
        var drawList = ImGui.GetWindowDrawList();
        if (_document != null)
        {
            for (int i = 0; i < _document.Frames.Count; i++)
            {
                var frame = _document.Frames[i];
                var min = cursorPos + new Vector2(frame.X * fitScale, frame.Y * fitScale);
                var max = min + new Vector2(frame.Width * fitScale, frame.Height * fitScale);

                uint color = (i == _selectedFrame)
                    ? ImGui.GetColorU32(new Vector4(1f, 1f, 0f, 0.8f))
                    : ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1f, 0.5f));

                drawList.AddRect(min, max, color, 0f, ImDrawFlags.None, 1.5f);

                // Frame index label
                var labelPos = min + new Vector2(2, 2);
                drawList.AddText(labelPos, color, i.ToString());
            }
        }

        // Draw pivot crosshair for selected frame
        if (_document != null && _selectedFrame >= 0 && _selectedFrame < _document.Frames.Count)
        {
            var selFrame = _document.Frames[_selectedFrame];
            float pivotScreenX = cursorPos.X + (selFrame.X + selFrame.PivotX * selFrame.Width) * fitScale;
            float pivotScreenY = cursorPos.Y + (selFrame.Y + selFrame.PivotY * selFrame.Height) * fitScale;
            var pivotPos = new Vector2(pivotScreenX, pivotScreenY);
            float crossSize = 12f;
            uint pivotColor = ImGui.GetColorU32(new Vector4(1f, 0.2f, 0.2f, 0.9f));

            // Crosshair lines
            drawList.AddLine(
                pivotPos - new Vector2(crossSize, 0), pivotPos + new Vector2(crossSize, 0),
                pivotColor, 2f);
            drawList.AddLine(
                pivotPos - new Vector2(0, crossSize), pivotPos + new Vector2(0, crossSize),
                pivotColor, 2f);
            // Circle at center
            drawList.AddCircle(pivotPos, 4f, pivotColor, 12, 2f);

            // Pivot drag interaction
            var mousePos = ImGui.GetMousePos();
            float distToPivot = Vector2.Distance(mousePos, pivotPos);
            if (distToPivot < 10f && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered())
            {
                _isDraggingPivot = true;
            }
        }

        if (_isDraggingPivot && _document != null && _selectedFrame >= 0 && _selectedFrame < _document.Frames.Count)
        {
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                var mousePos = ImGui.GetMousePos() - cursorPos;
                var selFrame = _document.Frames[_selectedFrame];
                float localX = mousePos.X / fitScale - selFrame.X;
                float localY = mousePos.Y / fitScale - selFrame.Y;
                selFrame.PivotX = Math.Clamp(localX / selFrame.Width, 0f, 1f);
                selFrame.PivotY = Math.Clamp(localY / selFrame.Height, 0f, 1f);
            }
            else
            {
                _isDraggingPivot = false;
            }
        }

        // Click to select frame (skip if dragging pivot)
        if (!_isDraggingPivot && ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && _document != null)
        {
            var mousePos = ImGui.GetMousePos() - cursorPos;
            int texX = (int)(mousePos.X / fitScale);
            int texY = (int)(mousePos.Y / fitScale);

            for (int i = 0; i < _document.Frames.Count; i++)
            {
                var f = _document.Frames[i];
                if (texX >= f.X && texX < f.X + f.Width &&
                    texY >= f.Y && texY < f.Y + f.Height)
                {
                    _selectedFrame = i;
                    break;
                }
            }
        }

        // Zoom with scroll
        if (ImGui.IsItemHovered())
        {
            float scroll = ImGui.GetIO().MouseWheel;
            if (scroll != 0)
                _zoom = Math.Clamp(_zoom + scroll * 0.1f, 0.25f, 5f);
        }
    }

    private void DrawFrameList()
    {
        ImGui.Text("Frames");
        ImGui.Separator();

        if (_document == null) return;

        for (int i = 0; i < _document.Frames.Count; i++)
        {
            var frame = _document.Frames[i];
            bool selected = _selectedFrame == i;
            ImGui.PushID(i);

            if (ImGui.Selectable($"{i}: {frame.Name}", selected))
                _selectedFrame = i;

            ImGui.PopID();
        }

        ImGui.Separator();

        // Edit selected frame
        if (_selectedFrame >= 0 && _selectedFrame < _document.Frames.Count)
        {
            var frame = _document.Frames[_selectedFrame];
            ImGui.Text($"Frame {_selectedFrame}");

            var name = frame.Name;
            if (ImGui.InputText("Name", ref name, 256))
                frame.Name = name;

            ImGui.DragInt("X", ref frame.X);
            ImGui.DragInt("Y", ref frame.Y);
            ImGui.DragInt("Width", ref frame.Width);
            ImGui.DragInt("Height", ref frame.Height);
            ImGui.DragFloat("Pivot X", ref frame.PivotX, 0.01f, 0f, 1f);
            ImGui.DragFloat("Pivot Y", ref frame.PivotY, 0.01f, 0f, 1f);
            ImGui.DragFloat("Duration", ref frame.Duration, 0.01f, 0.01f, 10f);

            if (ImGui.Button("Delete Frame"))
            {
                _document.Frames.RemoveAt(_selectedFrame);
                _selectedFrame = Math.Min(_selectedFrame, _document.Frames.Count - 1);
            }
        }
    }

    private void AutoSlice()
    {
        if (_document == null || _texture == null) return;

        _document.Frames.Clear();

        int frameW = _texture.Width / _sliceCols;
        int frameH = _texture.Height / _sliceRows;
        int index = 0;

        for (int row = 0; row < _sliceRows; row++)
        {
            for (int col = 0; col < _sliceCols; col++)
            {
                _document.Frames.Add(new SpriteFrame
                {
                    Name = $"frame_{index}",
                    X = col * frameW,
                    Y = row * frameH,
                    Width = frameW,
                    Height = frameH,
                    PivotX = 0.5f,
                    PivotY = 0.5f,
                    Duration = 0.1f
                });
                index++;
            }
        }

        Log.Info($"Auto-sliced into {index} frames ({_sliceCols}x{_sliceRows})");
    }

    private void Save()
    {
        if (_document == null || _currentPath == null) return;
        SpriteSheetSerializer.Save(_currentPath, _document);
        Log.Info($"Sprite sheet saved: {_currentPath}");
    }

    private void LoadTexture(string texturePath)
    {
        if (_textureCache == null || _imGui == null) return;

        // Unbind previous
        if (_textureRef.HasValue)
        {
            _imGui.UnbindTexture(_textureRef.Value);
            _textureRef = null;
        }

        _texture = _textureCache.Get(texturePath);
        if (_texture != null)
            _textureRef = _imGui.BindTexture(_texture);
    }
}
