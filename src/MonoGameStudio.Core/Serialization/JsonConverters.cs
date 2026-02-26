using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Serialization;

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new Vector2(
            root.GetProperty("x").GetSingle(),
            root.GetProperty("y").GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new Color(
            root.GetProperty("r").GetByte(),
            root.GetProperty("g").GetByte(),
            root.GetProperty("b").GetByte(),
            root.GetProperty("a").GetByte());
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("r", value.R);
        writer.WriteNumber("g", value.G);
        writer.WriteNumber("b", value.B);
        writer.WriteNumber("a", value.A);
        writer.WriteEndObject();
    }
}

public class RectangleConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new Rectangle(
            root.GetProperty("x").GetInt32(),
            root.GetProperty("y").GetInt32(),
            root.GetProperty("width").GetInt32(),
            root.GetProperty("height").GetInt32());
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("width", value.Width);
        writer.WriteNumber("height", value.Height);
        writer.WriteEndObject();
    }
}

public class MatrixConverter : JsonConverter<Matrix>
{
    public override Matrix Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read(); // skip
        return Matrix.Identity;
    }

    public override void Write(Utf8JsonWriter writer, Matrix value, JsonSerializerOptions options)
    {
        writer.WriteStringValue("identity");
    }
}
