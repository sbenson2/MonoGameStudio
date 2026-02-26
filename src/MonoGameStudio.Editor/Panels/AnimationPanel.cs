using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class AnimationPanel
{
    private AnimationDocument? _document;
    private string? _currentPath;
    private int _selectedClip;
    private int _selectedFrame = -1;

    // Playback preview state
    private bool _isPreviewPlaying;
    private float _previewTimer;
    private int _previewFrame;

    public void OpenAnimation(string path)
    {
        _document = AnimationSerializer.Load(path);
        _currentPath = path;
        _selectedClip = 0;
        _selectedFrame = -1;
        _isPreviewPlaying = false;
        Log.Info($"Opened animation: {path}");
    }

    public void CreateNew(string spriteSheetPath, string savePath)
    {
        _document = new AnimationDocument { SpriteSheetPath = spriteSheetPath };
        _document.Clips.Add(new AnimationClip { Name = "Idle" });
        _currentPath = savePath;
        _selectedClip = 0;
        _selectedFrame = -1;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.Animation, ref isOpen))
        {
            if (_document == null)
            {
                ImGui.TextDisabled("No animation open.");
                ImGui.TextDisabled("Create one from a sprite sheet.");
            }
            else
            {
                DrawToolbar();
                DrawClipList();
                ImGui.Separator();
                DrawTimeline();
                ImGui.Separator();
                DrawFrameProperties();
            }
        }
        ImGui.End();
    }

    public void UpdatePreview(float deltaTime)
    {
        if (!_isPreviewPlaying || _document == null) return;
        if (_selectedClip < 0 || _selectedClip >= _document.Clips.Count) return;

        var clip = _document.Clips[_selectedClip];
        if (clip.Frames.Count == 0) return;

        _previewTimer += deltaTime * clip.Speed;
        float frameDuration = clip.Frames[_previewFrame].Duration;

        if (_previewTimer >= frameDuration)
        {
            _previewTimer -= frameDuration;
            _previewFrame++;

            if (_previewFrame >= clip.Frames.Count)
            {
                if (clip.Loop)
                    _previewFrame = 0;
                else
                {
                    _previewFrame = clip.Frames.Count - 1;
                    _isPreviewPlaying = false;
                }
            }
        }
    }

    private void DrawToolbar()
    {
        // Playback controls
        if (_isPreviewPlaying)
        {
            if (ImGui.Button($"{FontAwesomeIcons.Pause}##anim_pause"))
                _isPreviewPlaying = false;
        }
        else
        {
            if (ImGui.Button($"{FontAwesomeIcons.Play}##anim_play"))
            {
                _isPreviewPlaying = true;
                _previewTimer = 0;
                _previewFrame = 0;
            }
        }
        ImGui.SameLine();

        if (ImGui.Button($"{FontAwesomeIcons.Stop}##anim_stop"))
        {
            _isPreviewPlaying = false;
            _previewFrame = 0;
            _previewTimer = 0;
        }
        ImGui.SameLine();

        // Preview frame indicator
        if (_document != null && _selectedClip >= 0 && _selectedClip < _document.Clips.Count)
        {
            var clip = _document.Clips[_selectedClip];
            ImGui.Text($"Frame: {_previewFrame}/{clip.Frames.Count}");
        }

        ImGui.SameLine();
        float rightEdge = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rightEdge - 60);

        if (ImGui.Button("Save"))
            Save();
    }

    private void DrawClipList()
    {
        if (_document == null) return;

        ImGui.Text("Clips");

        // Horizontal clip tabs
        for (int i = 0; i < _document.Clips.Count; i++)
        {
            if (i > 0) ImGui.SameLine();
            bool selected = _selectedClip == i;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

            if (ImGui.SmallButton($"{_document.Clips[i].Name}##{i}"))
            {
                _selectedClip = i;
                _previewFrame = 0;
                _previewTimer = 0;
            }

            if (selected) ImGui.PopStyleColor();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("+##addclip"))
        {
            _document.Clips.Add(new AnimationClip { Name = $"Clip_{_document.Clips.Count}" });
        }
    }

    private void DrawTimeline()
    {
        if (_document == null || _selectedClip < 0 || _selectedClip >= _document.Clips.Count) return;

        var clip = _document.Clips[_selectedClip];
        var availSize = ImGui.GetContentRegionAvail();
        float timelineHeight = 60f;
        float frameWidth = 50f;

        ImGui.Text("Timeline");

        var startPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        // Background
        var bgMin = startPos;
        var bgMax = startPos + new Vector2(availSize.X, timelineHeight);
        drawList.AddRectFilled(bgMin, bgMax,
            ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)));

        // Draw frame cells
        for (int i = 0; i < clip.Frames.Count; i++)
        {
            var frameMin = startPos + new Vector2(i * frameWidth, 0);
            var frameMax = frameMin + new Vector2(frameWidth - 2, timelineHeight);

            bool isSelected = _selectedFrame == i;
            bool isPreview = _previewFrame == i;

            var color = isSelected
                ? new Vector4(0.4f, 0.6f, 0.9f, 1f)
                : isPreview
                    ? new Vector4(0.3f, 0.7f, 0.3f, 1f)
                    : new Vector4(0.25f, 0.25f, 0.25f, 1f);

            drawList.AddRectFilled(frameMin, frameMax, ImGui.GetColorU32(color), 2f);
            drawList.AddRect(frameMin, frameMax,
                ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f)), 2f);

            // Frame label
            var label = clip.Frames[i].FrameName;
            if (label.Length > 6) label = label[..5] + "..";
            drawList.AddText(frameMin + new Vector2(4, 4),
                ImGui.GetColorU32(ImGuiCol.Text), label);

            // Duration
            drawList.AddText(frameMin + new Vector2(4, timelineHeight - 18),
                ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, 1f)),
                $"{clip.Frames[i].Duration:F2}s");
        }

        // Invisible button for click detection
        ImGui.SetCursorScreenPos(startPos);
        ImGui.InvisibleButton("##timeline", new Vector2(Math.Max(clip.Frames.Count * frameWidth, availSize.X), timelineHeight));

        if (ImGui.IsItemClicked())
        {
            var mouseX = ImGui.GetMousePos().X - startPos.X;
            int clickedFrame = (int)(mouseX / frameWidth);
            if (clickedFrame >= 0 && clickedFrame < clip.Frames.Count)
                _selectedFrame = clickedFrame;
        }

        // Add frame button
        if (ImGui.Button("+ Add Frame"))
        {
            clip.Frames.Add(new AnimationFrameRef
            {
                FrameName = $"frame_{clip.Frames.Count}",
                Duration = 0.1f
            });
        }
    }

    private void DrawFrameProperties()
    {
        if (_document == null || _selectedClip < 0 || _selectedClip >= _document.Clips.Count) return;

        var clip = _document.Clips[_selectedClip];

        // Clip properties
        ImGui.Text("Clip Properties");
        var clipName = clip.Name;
        if (ImGui.InputText("Clip Name", ref clipName, 256))
            clip.Name = clipName;
        ImGui.Checkbox("Loop", ref clip.Loop);
        ImGui.DragFloat("Speed", ref clip.Speed, 0.05f, 0.1f, 10f);

        if (_selectedClip < _document.Clips.Count && _document.Clips.Count > 1)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Delete Clip"))
            {
                _document.Clips.RemoveAt(_selectedClip);
                _selectedClip = Math.Min(_selectedClip, _document.Clips.Count - 1);
                return;
            }
        }

        ImGui.Separator();

        // Selected frame properties
        if (_selectedFrame >= 0 && _selectedFrame < clip.Frames.Count)
        {
            ImGui.Text($"Frame {_selectedFrame}");
            var frame = clip.Frames[_selectedFrame];

            var frameName = frame.FrameName;
            if (ImGui.InputText("Frame Name", ref frameName, 256))
                frame.FrameName = frameName;

            ImGui.DragFloat("Duration##frame", ref frame.Duration, 0.01f, 0.01f, 10f);

            if (ImGui.SmallButton("Delete Frame"))
            {
                clip.Frames.RemoveAt(_selectedFrame);
                _selectedFrame = Math.Min(_selectedFrame, clip.Frames.Count - 1);
            }
        }
    }

    private void Save()
    {
        if (_document == null || _currentPath == null) return;
        AnimationSerializer.Save(_currentPath, _document);
        Log.Info($"Animation saved: {_currentPath}");
    }
}
