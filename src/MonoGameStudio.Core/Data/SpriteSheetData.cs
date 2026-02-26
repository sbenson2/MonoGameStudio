using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Data;

public class SpriteSheetDocument
{
    [JsonPropertyName("texturePath")]
    public string TexturePath { get; set; } = "";

    [JsonPropertyName("frames")]
    public List<SpriteFrame> Frames { get; set; } = new();
}

public class SpriteFrame
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    // Public fields for ImGui ref access
    [JsonPropertyName("x")]
    [JsonInclude]
    public int X;

    [JsonPropertyName("y")]
    [JsonInclude]
    public int Y;

    [JsonPropertyName("width")]
    [JsonInclude]
    public int Width;

    [JsonPropertyName("height")]
    [JsonInclude]
    public int Height;

    [JsonPropertyName("pivotX")]
    [JsonInclude]
    public float PivotX = 0.5f;

    [JsonPropertyName("pivotY")]
    [JsonInclude]
    public float PivotY = 0.5f;

    [JsonPropertyName("duration")]
    [JsonInclude]
    public float Duration = 0.1f;

    public Rectangle ToRectangle() => new(X, Y, Width, Height);
    public Vector2 ToPivot() => new(PivotX, PivotY);
}

public static class SpriteSheetSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static SpriteSheetDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpriteSheetDocument>(json, _options);
    }

    public static void Save(string path, SpriteSheetDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, _options);
        File.WriteAllText(path, json);
    }
}
