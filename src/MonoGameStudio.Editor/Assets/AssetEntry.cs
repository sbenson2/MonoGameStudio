namespace MonoGameStudio.Editor.Assets;

public enum AssetType
{
    Unknown,
    Texture,
    Audio,
    Scene,
    Prefab,
    Font,
    SpriteSheet,
    Animation,
    Tilemap,
    Folder
}

public class AssetEntry
{
    public string FullPath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public AssetType Type { get; set; } = AssetType.Unknown;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirectory { get; set; }

    public static AssetType ClassifyExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga" => AssetType.Texture,
            ".wav" or ".ogg" or ".mp3" or ".flac" => AssetType.Audio,
            ".scene.json" => AssetType.Scene,
            ".prefab.json" => AssetType.Prefab,
            ".ttf" or ".otf" => AssetType.Font,
            ".spritesheet.json" => AssetType.SpriteSheet,
            ".animation.json" => AssetType.Animation,
            ".tilemap.json" => AssetType.Tilemap,
            _ => AssetType.Unknown
        };
    }

    /// <summary>
    /// Classify with multi-extension awareness (e.g., ".scene.json").
    /// </summary>
    public static AssetType ClassifyFile(string fileName)
    {
        if (fileName.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase)) return AssetType.Scene;
        if (fileName.EndsWith(".prefab.json", StringComparison.OrdinalIgnoreCase)) return AssetType.Prefab;
        if (fileName.EndsWith(".spritesheet.json", StringComparison.OrdinalIgnoreCase)) return AssetType.SpriteSheet;
        if (fileName.EndsWith(".animation.json", StringComparison.OrdinalIgnoreCase)) return AssetType.Animation;
        if (fileName.EndsWith(".tilemap.json", StringComparison.OrdinalIgnoreCase)) return AssetType.Tilemap;

        var ext = Path.GetExtension(fileName);
        return ClassifyExtension(ext);
    }
}
