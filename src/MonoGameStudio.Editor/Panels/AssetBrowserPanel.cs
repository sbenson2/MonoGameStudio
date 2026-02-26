using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.Assets;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class AssetBrowserPanel
{
    private AssetDatabase? _assetDatabase;
    private TextureCache? _textureCache;
    private ImGuiManager? _imGui;

    // State
    private string _currentDirectory = "";
    private string _searchQuery = "";
    private int _typeFilter; // 0=All, 1=Textures, 2=Audio, 3=Scenes
    private bool _gridView = true;

    // Thumbnail cache (path → ImGui texture ref)
    private readonly Dictionary<string, ImTextureRef> _thumbnailCache = new();
    private readonly HashSet<string> _thumbnailFailed = new();

    // Filter tab definitions
    private static readonly (string Label, AssetType? Filter)[] _filterTabs =
    {
        ("All", null),
        ("Textures", AssetType.Texture),
        ("Audio", AssetType.Audio),
        ("Scenes", AssetType.Scene),
    };

    public void Initialize(AssetDatabase assetDatabase, TextureCache textureCache, ImGuiManager imGui)
    {
        _assetDatabase = assetDatabase;
        _textureCache = textureCache;
        _imGui = imGui;
    }

    public void SetProjectRoot(string? projectDirectory)
    {
        ClearThumbnails();

        if (projectDirectory == null)
        {
            _assetDatabase?.Clear();
            _currentDirectory = "";
            return;
        }

        _currentDirectory = projectDirectory;
        _assetDatabase?.SetRoot(projectDirectory);
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.AssetBrowser, ref isOpen))
        {
            if (_assetDatabase == null || !_assetDatabase.HasRoot)
            {
                ImGui.TextDisabled("No project open");
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
        // Search bar
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##search", $"{FontAwesomeIcons.Search} Search...",
            ref _searchQuery, 256);

        ImGui.SameLine();

        // Type filter tabs
        for (int i = 0; i < _filterTabs.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            bool selected = _typeFilter == i;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            if (ImGui.SmallButton(_filterTabs[i].Label))
                _typeFilter = i;
            if (selected) ImGui.PopStyleColor();
        }

        ImGui.SameLine();
        float rightEdge = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rightEdge - 50);

        // View toggle
        if (ImGui.SmallButton(_gridView ? "List" : "Grid"))
            _gridView = !_gridView;

        ImGui.Separator();
    }

    private void DrawContent()
    {
        float availWidth = ImGui.GetContentRegionAvail().X;
        float treeWidth = Math.Max(150, availWidth * 0.22f);

        // Left pane: folder tree
        if (ImGui.BeginChild("FolderTree", new Vector2(treeWidth, 0), ImGuiChildFlags.Borders))
        {
            DrawFolderTree(_assetDatabase!.RootDirectory!);
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Right pane: file list/grid
        if (ImGui.BeginChild("FileView", Vector2.Zero))
        {
            DrawBreadcrumbs();
            ImGui.Separator();

            if (_gridView)
                DrawGridView();
            else
                DrawListView();
        }
        ImGui.EndChild();
    }

    private void DrawFolderTree(string rootDir)
    {
        var rootName = Path.GetFileName(rootDir);
        if (string.IsNullOrEmpty(rootName)) rootName = "Project";

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
        if (_currentDirectory == rootDir)
            flags |= ImGuiTreeNodeFlags.Selected;

        bool open = ImGui.TreeNodeEx($"{FontAwesomeIcons.FolderOpen} {rootName}##{rootDir}", flags);

        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
            _currentDirectory = rootDir;

        if (open)
        {
            DrawFolderTreeChildren(rootDir);
            ImGui.TreePop();
        }
    }

    private void DrawFolderTreeChildren(string directory)
    {
        var subdirs = _assetDatabase!.GetSubdirectories(directory);
        foreach (var subDir in subdirs)
        {
            var dirName = Path.GetFileName(subDir);
            var flags = ImGuiTreeNodeFlags.OpenOnArrow;

            if (_currentDirectory == subDir)
                flags |= ImGuiTreeNodeFlags.Selected;

            // Check if has subdirectories
            var childDirs = _assetDatabase.GetSubdirectories(subDir);
            if (childDirs.Count == 0)
                flags |= ImGuiTreeNodeFlags.Leaf;

            bool open = ImGui.TreeNodeEx($"{FontAwesomeIcons.FolderOpen} {dirName}##{subDir}", flags);

            if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
                _currentDirectory = subDir;

            if (open)
            {
                DrawFolderTreeChildren(subDir);
                ImGui.TreePop();
            }
        }
    }

    private void DrawBreadcrumbs()
    {
        if (_assetDatabase?.RootDirectory == null || string.IsNullOrEmpty(_currentDirectory))
            return;

        var relativePath = Path.GetRelativePath(_assetDatabase.RootDirectory, _currentDirectory);
        var parts = relativePath == "." ? Array.Empty<string>() : relativePath.Split(Path.DirectorySeparatorChar);

        // Root link
        if (ImGui.SmallButton("Project"))
            _currentDirectory = _assetDatabase.RootDirectory;

        string buildPath = _assetDatabase.RootDirectory;
        foreach (var part in parts)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("/");
            ImGui.SameLine();

            buildPath = Path.Combine(buildPath, part);
            var fullPath = buildPath; // capture for lambda
            if (ImGui.SmallButton(part))
                _currentDirectory = fullPath;
        }
    }

    private void DrawGridView()
    {
        var entries = GetFilteredEntries();
        float itemSize = 80f;
        float padding = 8f;
        float cellWidth = itemSize + padding;
        float availWidth = ImGui.GetContentRegionAvail().X;
        int columns = Math.Max(1, (int)(availWidth / cellWidth));

        // Draw subdirectories first
        if (_assetDatabase != null)
        {
            var subdirs = _assetDatabase.GetSubdirectories(_currentDirectory);
            foreach (var subDir in subdirs)
            {
                var dirName = Path.GetFileName(subDir);
                DrawGridItem(dirName, FontAwesomeIcons.FolderOpen, null, subDir, true, itemSize);

                int col = (int)((ImGui.GetCursorPosX() + cellWidth) / cellWidth);
                if (col < columns)
                    ImGui.SameLine();
            }
        }

        // Draw files
        foreach (var entry in entries)
        {
            var icon = GetIconForType(entry.Type);
            ImTextureRef? thumbnail = null;

            if (entry.Type == AssetType.Texture)
                thumbnail = GetOrLoadThumbnail(entry.FullPath);

            DrawGridItem(entry.FileName, icon, thumbnail, entry.FullPath, false, itemSize);

            int col = (int)((ImGui.GetCursorPosX() + cellWidth) / cellWidth);
            if (col < columns)
                ImGui.SameLine();
        }
    }

    private void DrawGridItem(string name, string icon, ImTextureRef? thumbnail, string fullPath,
        bool isDirectory, float size)
    {
        ImGui.PushID(fullPath);

        var startPos = ImGui.GetCursorPos();
        if (ImGui.Selectable($"##{fullPath}", false, ImGuiSelectableFlags.AllowDoubleClick,
                new Vector2(size, size + 20)))
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if (isDirectory)
                    _currentDirectory = fullPath;
            }
        }

        // Drag source for files
        if (!isDirectory && ImGui.BeginDragDropSource())
        {
            unsafe
            {
                var pathBytes = System.Text.Encoding.UTF8.GetBytes(fullPath + '\0');
                fixed (byte* ptr = pathBytes)
                {
                    ImGui.SetDragDropPayload("ASSET_PATH", ptr, (nuint)pathBytes.Length);
                }
            }
            ImGui.Text(name);
            ImGui.EndDragDropSource();
        }

        // Context menu
        if (ImGui.BeginPopupContextItem())
        {
            if (!isDirectory)
            {
                if (ImGui.MenuItem("Show in Finder"))
                    RevealInFinder(fullPath);
            }
            else
            {
                if (ImGui.MenuItem("Open"))
                    _currentDirectory = fullPath;
            }
            ImGui.EndPopup();
        }

        // Draw thumbnail or icon overlay
        var itemMin = ImGui.GetItemRectMin();
        var drawList = ImGui.GetWindowDrawList();

        if (thumbnail.HasValue)
        {
            var imgMin = itemMin + new Vector2(4, 4);
            var imgMax = imgMin + new Vector2(size - 8, size - 8);
            drawList.AddImage(thumbnail.Value, imgMin, imgMax);
        }
        else
        {
            // Draw icon centered
            var iconSize = ImGui.CalcTextSize(icon);
            var iconPos = itemMin + new Vector2((size - iconSize.X) / 2f, (size - iconSize.Y) / 2f);
            drawList.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), icon);
        }

        // Draw file name below
        var nameDisplay = name.Length > 12 ? name[..10] + ".." : name;
        var nameSize = ImGui.CalcTextSize(nameDisplay);
        var namePos = itemMin + new Vector2((size - nameSize.X) / 2f, size + 2);
        drawList.AddText(namePos, ImGui.GetColorU32(ImGuiCol.Text), nameDisplay);

        ImGui.PopID();
    }

    private void DrawListView()
    {
        var entries = GetFilteredEntries();

        // Subdirectories
        if (_assetDatabase != null)
        {
            var subdirs = _assetDatabase.GetSubdirectories(_currentDirectory);
            foreach (var subDir in subdirs)
            {
                var dirName = Path.GetFileName(subDir);
                if (ImGui.Selectable($"{FontAwesomeIcons.FolderOpen}  {dirName}",
                        false, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        _currentDirectory = subDir;
                }
            }
        }

        // Files
        foreach (var entry in entries)
        {
            var icon = GetIconForType(entry.Type);
            ImGui.PushID(entry.FullPath);

            if (ImGui.Selectable($"{icon}  {entry.FileName}", false, ImGuiSelectableFlags.None))
            {
                // Single click — could be used for preview selection
            }

            // Drag source
            if (ImGui.BeginDragDropSource())
            {
                unsafe
                {
                    var pathBytes = System.Text.Encoding.UTF8.GetBytes(entry.FullPath + '\0');
                    fixed (byte* ptr = pathBytes)
                    {
                        ImGui.SetDragDropPayload("ASSET_PATH", ptr, (nuint)pathBytes.Length);
                    }
                }
                ImGui.Text(entry.FileName);
                ImGui.EndDragDropSource();
            }

            // Context menu
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("Show in Finder"))
                    RevealInFinder(entry.FullPath);
                if (ImGui.MenuItem("Delete"))
                    TryDeleteFile(entry.FullPath);
                ImGui.EndPopup();
            }

            // Extra columns
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 120);
            ImGui.TextDisabled(FormatSize(entry.Size));
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 50);
            ImGui.TextDisabled(entry.Type.ToString());

            ImGui.PopID();
        }
    }

    private List<AssetEntry> GetFilteredEntries()
    {
        if (_assetDatabase == null) return new();

        var filter = _filterTabs[_typeFilter].Filter;

        if (!string.IsNullOrEmpty(_searchQuery) || filter.HasValue)
        {
            return _assetDatabase.Search(_searchQuery, filter);
        }

        return _assetDatabase.GetEntriesInDirectory(_currentDirectory);
    }

    private ImTextureRef? GetOrLoadThumbnail(string fullPath)
    {
        if (_textureCache == null || _imGui == null) return null;
        if (_thumbnailFailed.Contains(fullPath)) return null;

        if (_thumbnailCache.TryGetValue(fullPath, out var cached))
            return cached;

        var texture = _textureCache.Get(fullPath);
        if (texture == null)
        {
            _thumbnailFailed.Add(fullPath);
            return null;
        }

        var texRef = _imGui.BindTexture(texture);
        _thumbnailCache[fullPath] = texRef;
        return texRef;
    }

    private void ClearThumbnails()
    {
        if (_imGui != null)
        {
            foreach (var texRef in _thumbnailCache.Values)
                _imGui.UnbindTexture(texRef);
        }
        _thumbnailCache.Clear();
        _thumbnailFailed.Clear();
    }

    private static string GetIconForType(AssetType type) => type switch
    {
        AssetType.Texture => FontAwesomeIcons.Image,
        AssetType.Audio => FontAwesomeIcons.VolumeUp,
        AssetType.Scene => FontAwesomeIcons.Film,
        AssetType.Prefab => FontAwesomeIcons.Cubes,
        AssetType.Font => FontAwesomeIcons.Font,
        _ => FontAwesomeIcons.File,
    };

    private static void RevealInFinder(string path)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
            else if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start("explorer", $"/select,\"{path}\"");
            else
                System.Diagnostics.Process.Start("xdg-open", $"\"{Path.GetDirectoryName(path)}\"");
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to reveal in finder: {ex.Message}");
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            _assetDatabase?.Refresh();
            Log.Info($"Deleted: {path}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete: {ex.Message}");
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
