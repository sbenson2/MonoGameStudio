using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Physics;

public class CollisionLayerSettings
{
    public const int MaxLayers = 32;

    [JsonPropertyName("layerNames")]
    public string[] LayerNames { get; set; } = new string[MaxLayers];

    [JsonPropertyName("collisionMasks")]
    public int[] CollisionMasks { get; set; } = new int[MaxLayers];

    public CollisionLayerSettings()
    {
        for (int i = 0; i < MaxLayers; i++)
        {
            LayerNames[i] = i == 0 ? "Default" : $"Layer {i}";
            CollisionMasks[i] = ~0; // collide with everything by default
        }
    }

    public void SetCollision(int layerA, int layerB, bool enabled)
    {
        if (layerA < 0 || layerA >= MaxLayers || layerB < 0 || layerB >= MaxLayers) return;

        if (enabled)
        {
            CollisionMasks[layerA] |= (1 << layerB);
            CollisionMasks[layerB] |= (1 << layerA);
        }
        else
        {
            CollisionMasks[layerA] &= ~(1 << layerB);
            CollisionMasks[layerB] &= ~(1 << layerA);
        }
    }

    public bool GetCollision(int layerA, int layerB)
    {
        if (layerA < 0 || layerA >= MaxLayers || layerB < 0 || layerB >= MaxLayers) return false;
        return (CollisionMasks[layerA] & (1 << layerB)) != 0;
    }
}
