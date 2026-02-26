using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

public enum SpriteFilterMode { Point, Linear }
public enum TextureWrapMode { Clamp, Wrap }

public class TextureImportSettings
{
    [JsonPropertyName("filterMode")]
    public SpriteFilterMode FilterMode { get; set; } = SpriteFilterMode.Point;

    [JsonPropertyName("wrapMode")]
    public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.Clamp;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static string GetSidecarPath(string texturePath) => texturePath + ".import.json";

    public static TextureImportSettings Load(string texturePath)
    {
        var path = GetSidecarPath(texturePath);
        if (!File.Exists(path)) return new TextureImportSettings();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TextureImportSettings>(json, _options) ?? new TextureImportSettings();
        }
        catch { return new TextureImportSettings(); }
    }

    public void Save(string texturePath)
    {
        var path = GetSidecarPath(texturePath);
        var json = JsonSerializer.Serialize(this, _options);
        File.WriteAllText(path, json);
    }
}
