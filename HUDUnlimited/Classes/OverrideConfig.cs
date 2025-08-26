using System;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiLib.Classes;

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
    
    public void DrawConfig() {
        using var disabled = ImRaii.Disabled(!OverrideEnabled);

        using var table = ImRaii.Table("node_edit_table", 2);
        if (!table) return;

        using var id = ImRaii.PushId(NodePath);
        var configChanged = false;

        ImGui.TableSetupColumn("##enableColumn", ImGuiTableColumnFlags.WidthFixed, 25.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##option", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1.0f)) {
            using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 3.0f * ImGuiHelpers.GlobalScale);
                ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Enable Override");
            }
        }

        configChanged |= DrawCustomNameField();
        configChanged |= DrawFloat2Option("Position", ref Position, OverrideFlags.Position);
        configChanged |= DrawFloat2Option("Scale", ref Scale, OverrideFlags.Scale, 0.01f);
        configChanged |= DrawColor4Option("Color", ref Color, OverrideFlags.Color);
        configChanged |= DrawColor3Option("Add Color", ref AddColor, OverrideFlags.AddColor);
        configChanged |= DrawColor3Option("Subtract Color", ref SubtractColor, OverrideFlags.SubtractColor);
        configChanged |= DrawColor3Option("Multiply Color", ref MultiplyColor, OverrideFlags.MultiplyColor);
        configChanged |= DrawBoolOption("Visibility", ref Visible, OverrideFlags.Visibility);

        if (IsTextNode ?? false) {
            configChanged |= DrawTextNodeConfig();
        }

        if (configChanged) {
            System.Config.Save();
        }
    }

    private bool DrawTextNodeConfig() {
        var configChanged = false;
        
        configChanged |= DrawColor4Option("Text Color", ref TextColor, OverrideFlags.TextColor);
        configChanged |= DrawColor4Option("Text Outline Color", ref TextOutlineColor, OverrideFlags.TextOutlineColor);
        configChanged |= DrawColor4Option("Text Background Color", ref TextBackgroundColor, OverrideFlags.TextBackgroundColor);
        configChanged |= DrawIntOption("Font Size", ref FontSize, OverrideFlags.FontSize);
        configChanged |= DrawEnumOption("Font Type", ref FontType, OverrideFlags.FontType);
        configChanged |= DrawEnumOption("Alignment Type", ref AlignmentType, OverrideFlags.AlignmentType);
        configChanged |= DrawEnumOption("Text Flags", ref TextFlags, OverrideFlags.TextFlags);

        return configChanged;
    }

    private bool DrawBoolOption(string label, ref bool value, OverrideFlags flags) {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);
        
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.Checkbox($"##{label}", ref value);
        }
        return configChanged;
    }

    private bool DrawColor3Option(string label, ref Vector3 color, OverrideFlags flags) {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);
        
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit3($"##{label}", ref color);
        }
        return configChanged;
    }

    private bool DrawColor4Option(string label, ref Vector4 value, OverrideFlags flags) {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit4($"##{label}", ref value, ImGuiColorEditFlags.AlphaPreviewHalf);
        }
        return configChanged;
    }

    private bool DrawFloat2Option(string label, ref Vector2 option, OverrideFlags flags, float speed = 1.0f) {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.DragFloat2($"##{label}", ref option, speed);
        }
        
        return configChanged;
    }

    private bool DrawIntOption(string label, ref int option, OverrideFlags flags) {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.InputInt($"##{label}", ref option);
        }
        
        return configChanged;
    }

    private bool DrawEnumOption<T>(string label, ref T option, OverrideFlags flags) where T : Enum {
        var configChanged = false;
        configChanged |= DrawOptionHeader(label, this, flags);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(flags))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGuiTweaks.EnumCombo($"##{label}", ref option);
        }
        
        return configChanged;
    }

    private bool DrawCustomNameField() {
        var configChanged = false;
        
        ImGui.TableNextColumn();
        ImGui.Text("Custom Name");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        configChanged |= ImGui.InputTextWithHint("##Name", NodePath, ref CustomName, 64);
        return configChanged;
    }

    private static bool DrawOptionHeader(string label, OverrideConfig? option, OverrideFlags flag) {
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(5.0f);
        
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text(label);
        
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        return DrawFlagOption(option, flag);
    }
    
    private static bool DrawFlagOption(OverrideConfig? option, OverrideFlags flag) {
        var positionFlag = option?.Flags.HasFlag(flag) ?? false;
        
        ImGui.SetCursorPosX(5.0f);
        if (ImGui.Checkbox($"##FlagOption{flag.ToString()}", ref positionFlag)) {
            if (option is not null) {
                if (positionFlag) {
                    option.Flags |= flag;
                }
                else {
                    option.Flags &= ~flag;
                }
            }

            return true;
        }

        return false;
    }

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