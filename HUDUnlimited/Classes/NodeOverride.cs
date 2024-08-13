using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace HUDUnlimited.Classes;

public class NodeOverride {
    public required string NodePath { get; set; }
    
    [JsonIgnore] public string AddonName => NodePath.Split("/")[0];
    
    public bool OverrideEnabled;
    
    public Vector2 Position;
    public Vector2 Scale;
    public Vector4 Color;
    public Vector3 AddColor;
    public Vector3 MultiplyColor;
    public bool Visible;
    
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