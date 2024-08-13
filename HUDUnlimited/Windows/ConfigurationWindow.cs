using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDUnlimited.Classes;
using ImGuiNET;
using KamiLib.Classes;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public unsafe class ConfigurationWindow() : Window("HUDUnlimited Configuration Window", new Vector2(800.0f, 600.0f)) {

    private AtkUnitBase* selectedAddon;
    private AtkResNode* selectedNode;
    private string selectedNodePath = string.Empty;

    private bool locateNode;

    protected override void DrawContents() {
        using var table = ImRaii.Table("configuration_table", 3, ImGuiTableFlags.Resizable);
        if (!table) return;
        
        ImGui.TableSetupColumn("##addon_selection", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##node_selection", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##node_configuration", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        using (var addonChild = ImRaii.Child("addon_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
            if (addonChild) {
                DrawAddonSelection();
            }
        }

        ImGui.TableNextColumn();
        using (var nodeChild = ImRaii.Child("node_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
            if (nodeChild) {
                DrawNodeSelection();
            }
        }
        
        ImGui.TableNextColumn();
        using (var nodeConfigChild = ImRaii.Child("node_config_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
            if (nodeConfigChild) {
                DrawNodeConfiguration();
            }
        }
    }

    private void DrawAddonSelection() {
        using var frameBg = ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBg] with { W = 0.10f });
        using var scrollBarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 0.0f);
             
        var extraButtonSize = new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale);
        var listBoxSize = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing - extraButtonSize;
             
        using (var listBox = ImRaii.ListBox("##addonSelectionListBox", listBoxSize)) {
            if (listBox) {
                using var headerHoverColor = ImRaii.PushColor(ImGuiCol.HeaderHovered, ImGui.GetStyle().Colors[(int) ImGuiCol.HeaderHovered] with { W = 0.1f });
                using var textSelectedColor = ImRaii.PushColor(ImGuiCol.Header, ImGui.GetStyle().Colors[(int) ImGuiCol.Header] with { W = 0.1f });

                ImGuiClip.ClippedDraw(System.AddonListController.Addons, pointer => {
                    
                    var cursorPosition = ImGui.GetCursorPos();
                    if (ImGui.Selectable($"##{pointer.GetHashCode()}", selectedAddon == pointer.Value, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()))) {
                        selectedAddon = pointer.Value;
                    }

                    ImGui.SetCursorPos(cursorPosition);
                    var color = !pointer.Value->IsVisible ? KnownColor.Gray.Vector() with { W = 0.66f } : KnownColor.LightGreen.Vector();
                    ImGui.TextColored(color, pointer.Value->NameString);
                    

                }, ImGui.GetTextLineHeight());
            }
        }
         
        using (var selectionListButtonChild = ImRaii.Child("addon_selection_list_button", ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing)) {
            if (selectionListButtonChild) {
                var text = System.Config.HideInactiveAddons ? "Show Inactive" : "Hide Inactive";
     
                if (ImGui.Button(text, ImGui.GetContentRegionAvail())) {
                    System.Config.HideInactiveAddons = !System.Config.HideInactiveAddons;
                    System.Config.Save();
                }
            }
        }
    }

    private void DrawNodeSelection() {
        using var frameBg = ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBg] with { W = 0.10f });

        var extraButtonSize = new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale);
        var listBoxSize = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing - extraButtonSize;
             
        using (var listBox = ImRaii.ListBox("##nodeSelectionListBox", listBoxSize)) {
            if (listBox) {
                using var headerHoverColor = ImRaii.PushColor(ImGuiCol.HeaderHovered, ImGui.GetStyle().Colors[(int) ImGuiCol.HeaderHovered] with { W = 0.1f });
                using var textSelectedColor = ImRaii.PushColor(ImGuiCol.Header, ImGui.GetStyle().Colors[(int) ImGuiCol.Header] with { W = 0.1f });
                
                if (selectedAddon is not null) {
                    DrawNodeRecursively(ref selectedAddon->UldManager, selectedAddon->NameString);
                }
            }
        }
         
        using (var selectionListButtonChild = ImRaii.Child("node_selection_list_button", ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing)) {
            if (selectionListButtonChild) {
                var text = System.Config.HideInactiveNodes ? "Show Inactive" : "Hide Inactive";
     
                if (ImGui.Button(text, ImGui.GetContentRegionAvail())) {
                    System.Config.HideInactiveNodes = !System.Config.HideInactiveNodes;
                    System.Config.Save();
                }
            }
        }
    }

    private void DrawNodeConfiguration() {
        if (selectedNode is null) return;
        
        DrawNodeHeader();
        
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(10.0f);
        var configChanged = DrawNodeOptions();

        DrawExtraOptions();
        
        var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == selectedNodePath);
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPos(ImGui.GetContentRegionMax() - buttonSize);

        var buttonText = option?.OverrideEnabled ?? false ? "Disable Overrides" : "Enable Overrides";
        
        ImGui.SetCursorPos(ImGui.GetContentRegionMax() - buttonSize);
        using (ImRaii.Child("enable_disable_button", ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing)) {
            var enableDisableSize = new Vector2(buttonSize.X - 64.0f * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemInnerSpacing.X, buttonSize.Y - ImGui.GetStyle().ItemInnerSpacing.Y);
            
            if (ImGui.Button(buttonText, enableDisableSize)) {

                // Create and enable new option
                if (option is null) {
                    var newOption = new NodeOverride {
                        NodePath = selectedNodePath,
                        OverrideEnabled = true,
                    };
                    
                    System.Config.Overrides.Add(newOption);
                    System.AddonController.EnableOverride(newOption);
                    System.Config.Save();
                }
                else {
                    if (option.OverrideEnabled) {
                        option.OverrideEnabled = false;
                        System.AddonController.DisableOverride(option);
                        System.Config.Save();
                    }
                    else {
                        option.OverrideEnabled = true;
                        System.AddonController.EnableOverride(option);
                        System.Config.Save();
                    }
                }
            }
            
            ImGui.SameLine();
            using (ImRaii.Disabled(option is null)) {
                using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                    if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetContentRegionAvail()) && option is not null) {
                        option.OverrideEnabled = false;
                        System.AddonController.DisableOverride(option);
                        System.Config.Overrides.Remove(option);
                    }
                }
            }
        }
        
        if (locateNode) {
            HighlightNode(selectedNode);
        }

        if (configChanged) {
            System.Config.Save();
        }
    }

    private void DrawNodeHeader() {
        if (selectedNode is null) return;
        
        ImGuiHelpers.CenteredText(selectedNode->Type.ToString());
        
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = 0.50f })) {
            ImGuiHelpers.CenteredText(selectedNodePath);
        }

    }

    private bool DrawNodeOptions() {
        if (selectedNode is null) return false;
        var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == selectedNodePath);

        if (option is null || !option.OverrideEnabled) {
            using (ImRaii.PushColor(ImGuiCol.Text, KnownColor.Orange.Vector())) {
                ImGuiHelpers.CenteredText("Editing is currently Disabled");
            }
        }

        using var disabled = ImRaii.Disabled(option is null || !option.OverrideEnabled);

        using var table = ImRaii.Table("node_edit_table", 2);
        if (!table) return false;

        using var id = ImRaii.PushId(selectedNodePath);

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
        ImGui.Text("Position");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        configChanged |= DrawFlagOption(option, OverrideFlags.Position);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.Position) ?? false)) {
            var position = option?.Flags.HasFlag(OverrideFlags.Position) ?? false ? option.Position : new Vector2(selectedNode->GetXFloat(), selectedNode->GetYFloat());
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.DragFloat2("##Position", ref position) && option is not null) {
                option.Position = position;
                configChanged = true;
            }
        }

        configChanged |= DrawOptionHeader("Scale", option, OverrideFlags.Scale);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.Scale) ?? false)) {
            var scale = option?.Flags.HasFlag(OverrideFlags.Scale) ?? false ? option.Scale : new Vector2(selectedNode->GetScaleX(), selectedNode->GetScaleY());
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.DragFloat2("##Scale", ref scale, 0.01f) && option is not null) {
                option.Scale = scale;
                configChanged = true;
            }
        } 
        
        configChanged |= DrawOptionHeader("Color", option, OverrideFlags.Color);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.Color) ?? false)) {
            var color = option?.Flags.HasFlag(OverrideFlags.Color) ?? false ? option.Color : new Vector4(selectedNode->Color.R, selectedNode->Color.G, selectedNode->Color.B, selectedNode->Color.A) / 255.0f;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ColorEdit4("##Color", ref color, ImGuiColorEditFlags.AlphaPreviewHalf) && option is not null) {
                option.Color = color;
                configChanged = true;
            }
        }
        
        configChanged |= DrawOptionHeader("Add Color", option, OverrideFlags.AddColor);
        
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.AddColor) ?? false)) {
            var addColor = option?.Flags.HasFlag(OverrideFlags.AddColor) ?? false ? option.AddColor : new Vector3(selectedNode->AddRed, selectedNode->AddGreen, selectedNode->AddBlue) / 255.0f;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ColorEdit3("##AddColor", ref addColor) && option is not null) {
                option.AddColor = addColor;
                configChanged = true;
            }
        }

        configChanged |= DrawOptionHeader("Multiply Color", option, OverrideFlags.MultiplyColor);

        ImGui.TableNextColumn();

        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.MultiplyColor) ?? false)) {
            var multiplyColor = option?.Flags.HasFlag(OverrideFlags.MultiplyColor) ?? false ? option.MultiplyColor : new Vector3(selectedNode->MultiplyRed, selectedNode->MultiplyGreen, selectedNode->MultiplyBlue) / 100.0f;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ColorEdit3("##MultiplyColor", ref multiplyColor) && option is not null) {
                option.MultiplyColor = multiplyColor;
                configChanged = true;
            }
        }

        configChanged |= DrawOptionHeader("Visibility", option, OverrideFlags.Visibility);

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!option?.Flags.HasFlag(OverrideFlags.Visibility) ?? false)) {
            var isVisible = option?.Flags.HasFlag(OverrideFlags.Visibility) ?? false ? option.Visible : selectedNode->IsVisible();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.Checkbox("##Visibility", ref isVisible) && option is not null) {
                option.Visible = isVisible;
                configChanged = true;
            }
        }

        return configChanged;
    }

    private bool DrawOptionHeader(string label, NodeOverride? option, OverrideFlags flag) {
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

    private void DrawExtraOptions() {
        if (selectedNode is null) return;
        
        ImGuiTweaks.Header("Additional Options");

        using (ImRaii.PushIndent()) {
            ImGui.Checkbox("Locate Note", ref locateNode);
        }
        
        ImGuiHelpers.ScaledDummy(5.0f);
    }

    private void DrawNodeRecursively(ref AtkUldManager uldManager, string currentPath) {
        var nodeSpan = new Span<Pointer<AtkResNode>>(uldManager.NodeList, uldManager.NodeListCount);

        foreach (var node in nodeSpan) {
            if (node.Value is null) continue;
            
            var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == $"{currentPath}/{node.Value->NodeId}");
            if (System.Config.HideInactiveNodes && !node.Value->IsVisible() && !(option?.OverrideEnabled ?? false)) continue;
            
            var isComponentNode = (uint) node.Value->Type > 1000;
            
            if (isComponentNode) {
                var componentNode = (AtkComponentNode*) node.Value;

                DrawNodeRecursively(ref componentNode->Component->UldManager, $"{currentPath}/{node.Value->NodeId}");
            }

            var cursorPosition = ImGui.GetCursorPos();
            if (ImGui.Selectable($"##{(nint)node.Value:X}", selectedNode == node.Value, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()))) {
                selectedNode = node.Value;
                selectedNodePath = $"{currentPath}/{node.Value->NodeId}";
            }

            ImGui.SetCursorPos(cursorPosition);
            var color = !node.Value->IsVisible() ? KnownColor.Gray.Vector() with { W = 0.66f } : KnownColor.LightGreen.Vector();
            ImGui.TextColored(color, isComponentNode ? "Component" : node.Value->Type.ToString());

            if (option is not null) {
                using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                    var symbolSize = ImGui.CalcTextSize(FontAwesomeIcon.StarHalfAlt.ToIconString());
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - symbolSize.X);
                    ImGui.Text(option.OverrideEnabled ? FontAwesomeIcon.Star.ToIconString() : FontAwesomeIcon.StarHalfAlt.ToIconString());
                }
            }
        }
    }

    private void HighlightNode(AtkResNode* node) {
        var viewportSize = ImGui.GetMainViewport().Size;
        var maskColor = ImGui.GetColorU32(KnownColor.Gray.Vector() with { W = 0.80f });
        var borderColor = ImGui.GetColorU32(KnownColor.Red.Vector());
             
        // Top
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(viewportSize.X, node->ScreenY), maskColor);

        // Left
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(node->ScreenX, viewportSize.Y), maskColor);

        // Right
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(node->ScreenX + node->Width * node->ScaleX, 0.0f), new Vector2(viewportSize.X, viewportSize.Y), maskColor);

        // Bottom
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, node->ScreenY + node->Height * node->ScaleY), new Vector2(viewportSize.X, viewportSize.Y), maskColor);
             
        // Border
        ImGui.GetBackgroundDrawList().AddRect(new Vector2(node->ScreenX, node->ScreenY) - Vector2.One, new Vector2(node->ScreenX + node->Width * node->ScaleX, node->ScreenY + node->Height * node->ScaleY) + Vector2.One, borderColor);
    }

    private bool DrawFlagOption(NodeOverride? option, OverrideFlags flag) {
        var positionFlag = option?.Flags.HasFlag(flag) ?? false;
        
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
}