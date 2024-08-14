using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace HUDUnlimited.Classes;

public class NodeOverride {
    public required string NodePath { get; set; }
    
    [JsonIgnore] public string AddonName => NodePath.Split("/")[0];
    
    public bool OverrideEnabled;
    
    public Vector2 Position = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public Vector4 Color = Vector4.One;
    public Vector3 AddColor = Vector3.Zero;
    public Vector3 MultiplyColor = Vector3.One;
    public bool Visible = true;
    
    public OverrideFlags Flags;
}

[Flags]
public enum OverrideFlags {
    Position = 1 << 0,
    Scale = 1 << 1,
    Color = 1 << 2,
    AddColor = 1 << 3,
    MultiplyColor = 1 << 4,
    Visibility = 1 << 5,
}