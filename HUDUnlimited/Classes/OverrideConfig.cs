using System;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

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

        ImGui.TableNextColumn();
        ImGui.Text("Custom Name");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        configChanged |= ImGui.InputTextWithHint("##Name", NodePath, ref CustomName, 64);
        configChanged |= DrawOptionHeader("Position", this, OverrideFlags.Position);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.Position))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.DragFloat2("##Position", ref Position);
        } 

        configChanged |= DrawOptionHeader("Scale", this, OverrideFlags.Scale);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.Scale))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.DragFloat2("##Scale", ref Scale, 0.01f);
        } 
        
        configChanged |= DrawOptionHeader("Color", this, OverrideFlags.Color);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.Color))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit4("##Color", ref Color, ImGuiColorEditFlags.AlphaPreviewHalf);
        }
        
        configChanged |= DrawOptionHeader("Add Color", this, OverrideFlags.AddColor);
        
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.AddColor))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit3("##AddColor", ref AddColor);
        }
        
        configChanged |= DrawOptionHeader("Subtract Color", this, OverrideFlags.SubtractColor);
        
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.SubtractColor))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit3("##SubtractColor", ref SubtractColor);
        }

        configChanged |= DrawOptionHeader("Multiply Color", this, OverrideFlags.MultiplyColor);

        ImGui.TableNextColumn();

        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.MultiplyColor))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.ColorEdit3("##MultiplyColor", ref MultiplyColor);
        }

        configChanged |= DrawOptionHeader("Visibility", this, OverrideFlags.Visibility);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!Flags.HasFlag(OverrideFlags.Visibility))) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            configChanged |= ImGui.Checkbox("##Visibility", ref Visible);
        }

        if (configChanged) {
            System.Config.Save();
        }
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
}