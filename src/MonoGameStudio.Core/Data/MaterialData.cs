using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Serialization;

namespace MonoGameStudio.Core.Data;

public class MaterialDocument
{
    [JsonPropertyName("effectPath")]
    public string EffectPath { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, MaterialParameterValue> Parameters { get; set; } = new();
}

/// <summary>
/// Wraps a material parameter with type information for JSON polymorphic serialization.
/// Supported types: float, Vector2, Vector3, Vector4, Color, bool, int, string (texture path).
/// </summary>
public class MaterialParameterValue
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "float";

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    public static MaterialParameterValue FromFloat(float v) => new()
    {
        Type = "float",
        Value = JsonSerializer.SerializeToElement(v)
    };

    public static MaterialParameterValue FromVector2(Vector2 v) => new()
    {
        Type = "vector2",
        Value = JsonSerializer.SerializeToElement(v, _vecOptions)
    };

    public static MaterialParameterValue FromVector3(Vector3 v) => new()
    {
        Type = "vector3",
        Value = JsonSerializer.SerializeToElement(new float[] { v.X, v.Y, v.Z })
    };

    public static MaterialParameterValue FromVector4(Vector4 v) => new()
    {
        Type = "vector4",
        Value = JsonSerializer.SerializeToElement(new float[] { v.X, v.Y, v.Z, v.W })
    };

    public static MaterialParameterValue FromColor(Color c) => new()
    {
        Type = "color",
        Value = JsonSerializer.SerializeToElement(c, _colorOptions)
    };

    public static MaterialParameterValue FromBool(bool v) => new()
    {
        Type = "bool",
        Value = JsonSerializer.SerializeToElement(v)
    };

    public static MaterialParameterValue FromInt(int v) => new()
    {
        Type = "int",
        Value = JsonSerializer.SerializeToElement(v)
    };

    public static MaterialParameterValue FromString(string v) => new()
    {
        Type = "string",
        Value = JsonSerializer.SerializeToElement(v)
    };

    public float AsFloat() => Value.GetSingle();
    public int AsInt() => Value.GetInt32();
    public bool AsBool() => Value.GetBoolean();
    public string AsString() => Value.GetString() ?? "";

    public Vector2 AsVector2()
    {
        return JsonSerializer.Deserialize<Vector2>(Value.GetRawText(), _vecOptions);
    }

    public Vector3 AsVector3()
    {
        var arr = JsonSerializer.Deserialize<float[]>(Value.GetRawText()) ?? [0, 0, 0];
        return new Vector3(arr.Length > 0 ? arr[0] : 0, arr.Length > 1 ? arr[1] : 0, arr.Length > 2 ? arr[2] : 0);
    }

    public Vector4 AsVector4()
    {
        var arr = JsonSerializer.Deserialize<float[]>(Value.GetRawText()) ?? [0, 0, 0, 0];
        return new Vector4(
            arr.Length > 0 ? arr[0] : 0, arr.Length > 1 ? arr[1] : 0,
            arr.Length > 2 ? arr[2] : 0, arr.Length > 3 ? arr[3] : 0);
    }

    public Color AsColor()
    {
        return JsonSerializer.Deserialize<Color>(Value.GetRawText(), _colorOptions);
    }

    private static readonly JsonSerializerOptions _vecOptions = new()
    {
        Converters = { new Vector2Converter() }
    };

    private static readonly JsonSerializerOptions _colorOptions = new()
    {
        Converters = { new ColorConverter() }
    };
}

public static class MaterialSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new Vector2Converter(), new ColorConverter() },
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static MaterialDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MaterialDocument>(json, _options);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load material: {path}: {ex.Message}");
            return null;
        }
    }

    public static void Save(string path, MaterialDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, _options);
        File.WriteAllText(path, json);
    }
}
