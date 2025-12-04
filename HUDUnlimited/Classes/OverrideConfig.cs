using System;
using System.Numerics;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDUnlimited.Classes;

public class OverrideConfig {
    public required string NodePath { get; set; }

    public string NodeName => $"{NodePath}{(CustomName != string.Empty ? $" ( {CustomName} )" : string.Empty)}##{NodePath}";
    public string? ProxyParentName { get; set; }

    public string CustomName = string.Empty;
    
    [JsonIgnore] public string AddonName => NodePath.Split("/")[0];
    [JsonIgnore] public string AttachAddonName => ProxyParentName ?? AddonName;

    public bool OverrideEnabled;
    
    public Vector2 Position = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public Vector4 Color = Vector4.One;
    public Vector3 AddColor = Vector3.Zero;
    public Vector3 SubtractColor = Vector3.Zero;
    public Vector3 MultiplyColor = Vector3.One;
    public bool Visible = true;
    
    public OverrideFlags Flags;

    public bool? IsTextNode = false;
    public Vector4 TextColor = Vector4.One;
    public Vector4 TextOutlineColor = Vector4.One;
    public Vector4 TextBackgroundColor = Vector4.One;
    public int FontSize = 12;
    public FontType FontType = FontType.Axis;
    public AlignmentType AlignmentType = AlignmentType.Left;
    public TextFlags TextFlags;

    public void CopyTo(OverrideConfig? other) {
        if (other is null) return;
        
        other.ProxyParentName = ProxyParentName;
        other.CustomName = CustomName;
        other.OverrideEnabled = OverrideEnabled;
        other.Position = Position;
        other.Scale = Scale;
        other.Color = Color;
        other.AddColor = AddColor;
        other.SubtractColor = SubtractColor;
        other.MultiplyColor = MultiplyColor;
        other.Visible = Visible;
        other.Flags = Flags;
        other.TextColor = TextColor;
        other.TextOutlineColor = TextOutlineColor;
        other.TextBackgroundColor = TextBackgroundColor;
        other.FontSize = FontSize;
        other.FontType = FontType;
        other.AlignmentType = AlignmentType;
        other.TextFlags = TextFlags;
    }
}

[Flags]
public enum OverrideFlags {
    Position = 1 << 0,
    Scale = 1 << 1,
    Color = 1 << 2,
    AddColor = 1 << 3,
    MultiplyColor = 1 << 4,
    Visibility = 1 << 5,
    SubtractColor = 1 << 6,
    TextColor = 1 << 7,
    TextOutlineColor = 1 << 8,
    TextBackgroundColor = 1 << 9,
    FontSize = 1 << 10,
    FontType = 1 << 11,
    AlignmentType  = 1 << 12,
    TextFlags = 1 << 13,
}
