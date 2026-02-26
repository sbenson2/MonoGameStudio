using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Static, NativeAOT-safe registration of all component types.
/// Replaces Assembly.GetExportedTypes() scanning with explicit descriptors.
/// </summary>
public static class ComponentRegistrations
{
    public static void RegisterAll()
    {
        // === Core Transform (always present, serialized, not addable) ===

        ComponentRegistry.Register(new ComponentDescriptor<Position>
        {
            Name = "Position",
            IsCoreTransform = true,
            Fields = new FieldDescriptor[]
            {
                FloatField("X", c => ((Position)c).X, (c, v) => { var p = (Position)c; p.X = (float)v!; return p; }),
                FloatField("Y", c => ((Position)c).Y, (c, v) => { var p = (Position)c; p.Y = (float)v!; return p; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<Rotation>
        {
            Name = "Rotation",
            IsCoreTransform = true,
            Fields = new FieldDescriptor[]
            {
                FloatField("Angle", c => ((Rotation)c).Angle, (c, v) => { var r = (Rotation)c; r.Angle = (float)v!; return r; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<Scale>
        {
            Name = "Scale",
            IsCoreTransform = true,
            Fields = new FieldDescriptor[]
            {
                FloatField("X", c => ((Scale)c).X, (c, v) => { var s = (Scale)c; s.X = (float)v!; return s; }),
                FloatField("Y", c => ((Scale)c).Y, (c, v) => { var s = (Scale)c; s.Y = (float)v!; return s; }),
            }
        });

        // === Internal types (not serialized, not addable) ===

        ComponentRegistry.Register(new ComponentDescriptor<LocalTransform>
        {
            Name = "LocalTransform",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        ComponentRegistry.Register(new ComponentDescriptor<WorldTransform>
        {
            Name = "WorldTransform",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        ComponentRegistry.Register(new ComponentDescriptor<EntityName>
        {
            Name = "EntityName",
            IsInternal = true,
            Fields = new FieldDescriptor[]
            {
                StringField("Name", c => ((EntityName)c).Name, (c, v) => { var e = (EntityName)c; e.Name = (string)v!; return e; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<EntityGuid>
        {
            Name = "EntityGuid",
            IsInternal = true,
            Fields = new FieldDescriptor[]
            {
                GuidField("Id", c => ((EntityGuid)c).Id, (c, v) => { var e = (EntityGuid)c; e.Id = (Guid)v!; return e; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<Parent>
        {
            Name = "Parent",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        ComponentRegistry.Register(new ComponentDescriptor<Children>
        {
            Name = "Children",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        ComponentRegistry.Register(new ComponentDescriptor<SelectedTag>
        {
            Name = "SelectedTag",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        ComponentRegistry.Register(new ComponentDescriptor<EditorOnlyTag>
        {
            Name = "EditorOnlyTag",
            IsInternal = true,
            Fields = Array.Empty<FieldDescriptor>()
        });

        // === Rendering ===

        ComponentRegistry.Register(new ComponentDescriptor<SpriteRenderer>
        {
            Name = "SpriteRenderer",
            Category = "Rendering",
            Fields = new FieldDescriptor[]
            {
                StringField("TexturePath",
                    c => ((SpriteRenderer)c).TexturePath,
                    (c, v) => { var s = (SpriteRenderer)c; s.TexturePath = (string)v!; return s; }),
                ColorField("Tint",
                    c => ((SpriteRenderer)c).Tint,
                    (c, v) => { var s = (SpriteRenderer)c; s.Tint = (Color)v!; return s; }),
                RectangleField("SourceRect",
                    c => ((SpriteRenderer)c).SourceRect,
                    (c, v) => { var s = (SpriteRenderer)c; s.SourceRect = (Rectangle)v!; return s; }),
                Vector2Field("Origin",
                    c => ((SpriteRenderer)c).Origin,
                    (c, v) => { var s = (SpriteRenderer)c; s.Origin = (Vector2)v!; return s; }),
                IntField("SortOrder",
                    c => ((SpriteRenderer)c).SortOrder,
                    (c, v) => { var s = (SpriteRenderer)c; s.SortOrder = (int)v!; return s; }),
                StringField("SortLayer",
                    c => ((SpriteRenderer)c).SortLayer,
                    (c, v) => { var s = (SpriteRenderer)c; s.SortLayer = (string)v!; return s; }),
                BoolField("FlipX",
                    c => ((SpriteRenderer)c).FlipX,
                    (c, v) => { var s = (SpriteRenderer)c; s.FlipX = (bool)v!; return s; }),
                BoolField("FlipY",
                    c => ((SpriteRenderer)c).FlipY,
                    (c, v) => { var s = (SpriteRenderer)c; s.FlipY = (bool)v!; return s; }),
                FloatField("Opacity",
                    c => ((SpriteRenderer)c).Opacity,
                    (c, v) => { var s = (SpriteRenderer)c; s.Opacity = (float)v!; return s; },
                    rangeMin: 0f, rangeMax: 1f),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<Animator>
        {
            Name = "Animator",
            Category = "Rendering",
            Fields = new FieldDescriptor[]
            {
                StringField("AnimationDataPath",
                    c => ((Animator)c).AnimationDataPath,
                    (c, v) => { var a = (Animator)c; a.AnimationDataPath = (string)v!; return a; }),
                StringField("CurrentState",
                    c => ((Animator)c).CurrentState,
                    (c, v) => { var a = (Animator)c; a.CurrentState = (string)v!; return a; }),
                FloatField("Speed",
                    c => ((Animator)c).Speed,
                    (c, v) => { var a = (Animator)c; a.Speed = (float)v!; return a; }),
                BoolField("IsPlaying",
                    c => ((Animator)c).IsPlaying,
                    (c, v) => { var a = (Animator)c; a.IsPlaying = (bool)v!; return a; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<Camera2D>
        {
            Name = "Camera2D",
            Category = "Rendering",
            Fields = new FieldDescriptor[]
            {
                FloatField("ZoomLevel",
                    c => ((Camera2D)c).ZoomLevel,
                    (c, v) => { var cam = (Camera2D)c; cam.ZoomLevel = (float)v!; return cam; }),
                BoolField("IsActive",
                    c => ((Camera2D)c).IsActive,
                    (c, v) => { var cam = (Camera2D)c; cam.IsActive = (bool)v!; return cam; }),
                RectangleField("Limits",
                    c => ((Camera2D)c).Limits,
                    (c, v) => { var cam = (Camera2D)c; cam.Limits = (Rectangle)v!; return cam; }),
                FloatField("Smoothing",
                    c => ((Camera2D)c).Smoothing,
                    (c, v) => { var cam = (Camera2D)c; cam.Smoothing = (float)v!; return cam; }),
                GuidField("FollowTargetGuid",
                    c => ((Camera2D)c).FollowTargetGuid,
                    (c, v) => { var cam = (Camera2D)c; cam.FollowTargetGuid = (Guid)v!; return cam; }),
                Vector2Field("DeadzoneSize",
                    c => ((Camera2D)c).DeadzoneSize,
                    (c, v) => { var cam = (Camera2D)c; cam.DeadzoneSize = (Vector2)v!; return cam; }),
                Vector2Field("LookAhead",
                    c => ((Camera2D)c).LookAhead,
                    (c, v) => { var cam = (Camera2D)c; cam.LookAhead = (Vector2)v!; return cam; }),
                FloatField("LookAheadSmoothing",
                    c => ((Camera2D)c).LookAheadSmoothing,
                    (c, v) => { var cam = (Camera2D)c; cam.LookAheadSmoothing = (float)v!; return cam; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<TilemapRenderer>
        {
            Name = "TilemapRenderer",
            Category = "Rendering",
            Fields = new FieldDescriptor[]
            {
                StringField("TilemapDataPath",
                    c => ((TilemapRenderer)c).TilemapDataPath,
                    (c, v) => { var t = (TilemapRenderer)c; t.TilemapDataPath = (string)v!; return t; }),
                IntField("SortOrder",
                    c => ((TilemapRenderer)c).SortOrder,
                    (c, v) => { var t = (TilemapRenderer)c; t.SortOrder = (int)v!; return t; }),
            }
        });

        // === Physics ===

        ComponentRegistry.Register(new ComponentDescriptor<BoxCollider>
        {
            Name = "BoxCollider",
            Category = "Physics",
            Fields = new FieldDescriptor[]
            {
                FloatField("Width",
                    c => ((BoxCollider)c).Width,
                    (c, v) => { var b = (BoxCollider)c; b.Width = (float)v!; return b; }),
                FloatField("Height",
                    c => ((BoxCollider)c).Height,
                    (c, v) => { var b = (BoxCollider)c; b.Height = (float)v!; return b; }),
                Vector2Field("Offset",
                    c => ((BoxCollider)c).Offset,
                    (c, v) => { var b = (BoxCollider)c; b.Offset = (Vector2)v!; return b; }),
                BoolField("IsTrigger",
                    c => ((BoxCollider)c).IsTrigger,
                    (c, v) => { var b = (BoxCollider)c; b.IsTrigger = (bool)v!; return b; }),
                IntField("CollisionLayer",
                    c => ((BoxCollider)c).CollisionLayer,
                    (c, v) => { var b = (BoxCollider)c; b.CollisionLayer = (int)v!; return b; }),
                IntField("CollisionMask",
                    c => ((BoxCollider)c).CollisionMask,
                    (c, v) => { var b = (BoxCollider)c; b.CollisionMask = (int)v!; return b; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<CircleCollider>
        {
            Name = "CircleCollider",
            Category = "Physics",
            Fields = new FieldDescriptor[]
            {
                FloatField("Radius",
                    c => ((CircleCollider)c).Radius,
                    (c, v) => { var cc = (CircleCollider)c; cc.Radius = (float)v!; return cc; }),
                Vector2Field("Offset",
                    c => ((CircleCollider)c).Offset,
                    (c, v) => { var cc = (CircleCollider)c; cc.Offset = (Vector2)v!; return cc; }),
                BoolField("IsTrigger",
                    c => ((CircleCollider)c).IsTrigger,
                    (c, v) => { var cc = (CircleCollider)c; cc.IsTrigger = (bool)v!; return cc; }),
                IntField("CollisionLayer",
                    c => ((CircleCollider)c).CollisionLayer,
                    (c, v) => { var cc = (CircleCollider)c; cc.CollisionLayer = (int)v!; return cc; }),
                IntField("CollisionMask",
                    c => ((CircleCollider)c).CollisionMask,
                    (c, v) => { var cc = (CircleCollider)c; cc.CollisionMask = (int)v!; return cc; }),
            }
        });

        ComponentRegistry.Register(new ComponentDescriptor<RigidBody2D>
        {
            Name = "RigidBody2D",
            Category = "Physics",
            Fields = new FieldDescriptor[]
            {
                FloatField("Mass",
                    c => ((RigidBody2D)c).Mass,
                    (c, v) => { var r = (RigidBody2D)c; r.Mass = (float)v!; return r; }),
                FloatField("GravityScale",
                    c => ((RigidBody2D)c).GravityScale,
                    (c, v) => { var r = (RigidBody2D)c; r.GravityScale = (float)v!; return r; }),
                FloatField("Damping",
                    c => ((RigidBody2D)c).Damping,
                    (c, v) => { var r = (RigidBody2D)c; r.Damping = (float)v!; return r; }),
                BoolField("IsKinematic",
                    c => ((RigidBody2D)c).IsKinematic,
                    (c, v) => { var r = (RigidBody2D)c; r.IsKinematic = (bool)v!; return r; }),
                BoolField("FixedRotation",
                    c => ((RigidBody2D)c).FixedRotation,
                    (c, v) => { var r = (RigidBody2D)c; r.FixedRotation = (bool)v!; return r; }),
                EnumField<BodyType>("BodyType",
                    c => ((RigidBody2D)c).BodyType,
                    (c, v) => { var r = (RigidBody2D)c; r.BodyType = (BodyType)v!; return r; }),
            }
        });

        // === Audio ===

        ComponentRegistry.Register(new ComponentDescriptor<AudioSource>
        {
            Name = "AudioSource",
            Category = "Audio",
            Fields = new FieldDescriptor[]
            {
                StringField("ClipPath",
                    c => ((AudioSource)c).ClipPath,
                    (c, v) => { var a = (AudioSource)c; a.ClipPath = (string)v!; return a; }),
                FloatField("Volume",
                    c => ((AudioSource)c).Volume,
                    (c, v) => { var a = (AudioSource)c; a.Volume = (float)v!; return a; },
                    rangeMin: 0f, rangeMax: 1f),
                FloatField("Pitch",
                    c => ((AudioSource)c).Pitch,
                    (c, v) => { var a = (AudioSource)c; a.Pitch = (float)v!; return a; }),
                BoolField("Loop",
                    c => ((AudioSource)c).Loop,
                    (c, v) => { var a = (AudioSource)c; a.Loop = (bool)v!; return a; }),
                BoolField("PlayOnStart",
                    c => ((AudioSource)c).PlayOnStart,
                    (c, v) => { var a = (AudioSource)c; a.PlayOnStart = (bool)v!; return a; }),
            }
        });

        // === UI ===

        ComponentRegistry.Register(new ComponentDescriptor<GumScreen>
        {
            Name = "GumScreen",
            Category = "UI",
            Fields = new FieldDescriptor[]
            {
                StringField("GumProjectPath",
                    c => ((GumScreen)c).GumProjectPath,
                    (c, v) => { var g = (GumScreen)c; g.GumProjectPath = (string)v!; return g; }),
                StringField("ScreenName",
                    c => ((GumScreen)c).ScreenName,
                    (c, v) => { var g = (GumScreen)c; g.ScreenName = (string)v!; return g; }),
                BoolField("IsActive",
                    c => ((GumScreen)c).IsActive,
                    (c, v) => { var g = (GumScreen)c; g.IsActive = (bool)v!; return g; }),
            }
        });

        // === Particles ===

        ComponentRegistry.Register(new ComponentDescriptor<ParticleEmitter>
        {
            Name = "ParticleEmitter",
            Category = "Particles",
            Fields = new FieldDescriptor[]
            {
                StringField("PresetPath",
                    c => ((ParticleEmitter)c).PresetPath,
                    (c, v) => { var p = (ParticleEmitter)c; p.PresetPath = (string)v!; return p; }),
                BoolField("IsEmitting",
                    c => ((ParticleEmitter)c).IsEmitting,
                    (c, v) => { var p = (ParticleEmitter)c; p.IsEmitting = (bool)v!; return p; }),
                BoolField("PlayOnStart",
                    c => ((ParticleEmitter)c).PlayOnStart,
                    (c, v) => { var p = (ParticleEmitter)c; p.PlayOnStart = (bool)v!; return p; }),
            }
        });

        // === Materials ===

        ComponentRegistry.Register(new ComponentDescriptor<MaterialComponent>
        {
            Name = "MaterialComponent",
            Category = "Rendering",
            Fields = new FieldDescriptor[]
            {
                StringField("MaterialPath",
                    c => ((MaterialComponent)c).MaterialPath,
                    (c, v) => { var m = (MaterialComponent)c; m.MaterialPath = (string)v!; return m; }),
            }
        });

        // === General ===

        ComponentRegistry.Register(new ComponentDescriptor<EntityTag>
        {
            Name = "EntityTag",
            Category = "General",
            Fields = new FieldDescriptor[]
            {
                StringField("Tag",
                    c => ((EntityTag)c).Tag,
                    (c, v) => { var t = (EntityTag)c; t.Tag = (string)v!; return t; }),
                IntField("Layer",
                    c => ((EntityTag)c).Layer,
                    (c, v) => { var t = (EntityTag)c; t.Layer = (int)v!; return t; }),
            }
        });
    }

    // === Field factory helpers ===

    private static FieldDescriptor FloatField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        float? rangeMin = null, float? rangeMax = null, string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Float,
            GetValue = get, SetValue = set,
            RangeMin = rangeMin, RangeMax = rangeMax,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor IntField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Int,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor BoolField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Bool,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor StringField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.String,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor Vector2Field(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Vector2,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor ColorField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Color,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor RectangleField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Rectangle,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor GuidField(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null)
        => new()
        {
            Name = name, Kind = FieldKind.Guid,
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };

    private static FieldDescriptor EnumField<TEnum>(string name,
        Func<object, object?> get, Func<object, object?, object> set,
        string? tooltip = null, string? header = null) where TEnum : struct, Enum
        => new()
        {
            Name = name, Kind = FieldKind.Enum,
            EnumType = typeof(TEnum),
            GetValue = get, SetValue = set,
            Tooltip = tooltip, Header = header
        };
}
