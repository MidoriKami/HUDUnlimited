using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDUnlimited.Classes;
using ImGuiNET;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public enum ExtendedNodeType {
    None = 0,
    Res = NodeType.Res,
    Image = NodeType.Image,
    Text = NodeType.Text,
    NineGrid = NodeType.NineGrid,
    Counter = NodeType.Counter,
    Collision = NodeType.Collision,
    ClippingMask = NodeType.ClippingMask,
    Component = 1000,
}

public unsafe class ConfigurationWindow : Window {

    private AtkUnitBase* selectedAddon;
    private AtkResNode* selectedNode;
    private string selectedNodePath = string.Empty;

    private bool locateNode;
    private ExtendedNodeType typeFilter = ExtendedNodeType.None;

    private string filter = string.Empty;
    private List<Pointer<AtkUnitBase>> Addons => RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries.ToArray()
        .Where(entry => entry.Value is not null && entry.Value->IsReady)
        .Where(entry => !(!entry.Value->IsVisible && System.Config.HideInactiveAddons))
        .Where(entry => filter == string.Empty || entry.Value->NameString.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public ConfigurationWindow() : base("HUDUnlimited Configuration Window", new Vector2(800.0f, 600.0f)) {
        TitleBarButtons.Add(new TitleBarButton {
            Click = _ => System.OverrideListWindow.UnCollapseOrToggle(),
            Icon = FontAwesomeIcon.Cog,
            ShowTooltip = () => ImGui.SetTooltip("Open Preset Browser"),
            IconOffset = new Vector2(2.0f, 1.0f),
        });
    }

    protected override void DrawContents() {
        using var table = ImRaii.Table("configuration_table", 3, ImGuiTableFlags.Resizable);
        if (!table) return;
        
        ImGui.TableSetupColumn("##addon_selection", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##node_selection", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##node_configuration", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        using (var filterChild = ImRaii.Child("filter_child", new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale))) {
            if (filterChild) {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint("##filter", "filter...", ref filter, 64, ImGuiInputTextFlags.AutoSelectAll);
            }
        }
        using (var addonChild = ImRaii.Child("addon_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
            if (addonChild) {
                DrawAddonSelection();
            }
        }

        ImGui.TableNextColumn();
        using (var nodeExtrasTable = ImRaii.Table("node_extras_table", 2)) {
            if (nodeExtrasTable) {
                ImGui.TableSetupColumn("##typeFilter", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##locatorButton", ImGuiTableColumnFlags.WidthFixed, 32.0f * ImGuiHelpers.GlobalScale);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                using (var typeCombo = ImRaii.Combo("##nodeTypeFilter", typeFilter.ToString(), ImGuiComboFlags.HeightLarge)) {
                    if (typeCombo) {
                        foreach (var enumValue in Enum.GetValues<ExtendedNodeType>()) {
                            if (ImGui.Selectable(enumValue.ToString(), typeFilter == enumValue)) {
                                typeFilter = enumValue;
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                    if (ImGui.Button(locateNode ? FontAwesomeIcon.EyeSlash.ToIconString() : FontAwesomeIcon.Eye.ToIconString())) { 
                        locateNode = !locateNode;
                    }
                }

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(locateNode ? "Hide Node Locator" : "Show Node Locator");
                }
            }
        }
        
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

                ImGuiClip.ClippedDraw(Addons, pointer => {
                    
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
        DrawNodeOptions();

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
                    var newOption = new OverrideConfig {
                        NodePath = selectedNodePath,
                        OverrideEnabled = true,
                        Position = new Vector2(selectedNode->GetXFloat(), selectedNode->GetYFloat()),
                        Scale = new Vector2(selectedNode->GetScaleX(), selectedNode->GetScaleY()),
                        Color = new Vector4(selectedNode->Color.R, selectedNode->Color.G, selectedNode->Color.B, selectedNode->Color.A) / 255.0f,
                        AddColor = new Vector3(selectedNode->AddRed, selectedNode->AddGreen, selectedNode->AddBlue) / 255.0f,
                        SubtractColor = Vector3.Zero,
                        MultiplyColor = new Vector3(selectedNode->MultiplyRed, selectedNode->MultiplyGreen, selectedNode->MultiplyBlue) / 255.0f,
                        Visible = selectedNode->IsVisible(),
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
                        System.Config.Save();
                    }
                }
            }
        }
        
        if (locateNode) {
            HighlightNode(selectedNode);
        }
    }

    private void DrawNodeHeader() {
        if (selectedNode is null) return;
        
        ImGuiHelpers.CenteredText(selectedNode->Type.ToString());
        
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = 0.50f })) {
            ImGuiHelpers.CenteredText(selectedNodePath);
        }

    }

    private void DrawNodeOptions() {
        if (selectedNode is null) return;
        var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == selectedNodePath);

        if (option is null || !option.OverrideEnabled) {
            using (ImRaii.PushColor(ImGuiCol.Text, KnownColor.Orange.Vector())) {
                ImGuiHelpers.ScaledDummy(10.0f);
                ImGuiHelpers.CenteredText("Editing is currently Disabled");
                ImGuiHelpers.ScaledDummy(10.0f);
                ImGuiHelpers.CenteredText("Enable Overrides for this Node to configure");
            }
        }

        if (option is { OverrideEnabled: true }) {
            option.DrawConfig();
        }
    }

    private void DrawNodeRecursively(ref AtkUldManager uldManager, string currentPath) {
        var nodeSpan = new Span<Pointer<AtkResNode>>(uldManager.NodeList, uldManager.NodeListCount);

        foreach (var node in nodeSpan) {
            if (node.Value is null) continue;
            
            var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == $"{currentPath}/{node.Value->NodeId}");
            if (System.Config.HideInactiveNodes && !node.Value->IsVisible() && !(option?.OverrideEnabled ?? false)) continue;
            
            var isComponentNode = (uint) node.Value->Type > 1000;
            var nodeType = node.Value->Type;
            var shouldShow = typeFilter switch {
                ExtendedNodeType.None => true,
                ExtendedNodeType.Res when nodeType is NodeType.Res => true,
                ExtendedNodeType.Image when nodeType is NodeType.Image => true,
                ExtendedNodeType.Text when nodeType is NodeType.Text => true,
                ExtendedNodeType.NineGrid when nodeType is NodeType.NineGrid => true,
                ExtendedNodeType.Counter when nodeType is NodeType.Counter => true,
                ExtendedNodeType.Collision when nodeType is NodeType.Collision => true,
                ExtendedNodeType.ClippingMask when nodeType is NodeType.ClippingMask => true,
                ExtendedNodeType.Component when (int)node.Value->Type > 1000 => true,
                _ => false,
            };

            if (shouldShow) {
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
            
            if (isComponentNode) {
                if (shouldShow) {
                    ImGui.Indent(5.0f);
                }
                
                var componentNode = (AtkComponentNode*) node.Value;
                DrawNodeRecursively(ref componentNode->Component->UldManager, $"{currentPath}/{node.Value->NodeId}");
                
                if (shouldShow) {
                    ImGui.Indent(-5.0f);
                }
            }
        }
    }

    private void HighlightNode(AtkResNode* node) {
        if (node is null) return;
        
        var viewportSize = ImGui.GetMainViewport().Size;
        var maskColor = ImGui.GetColorU32(KnownColor.Gray.Vector() with { W = 0.80f });
        var borderColor = ImGui.GetColorU32(KnownColor.Red.Vector());

        var nodeScale = DrawHelpers.GetNodeScale(node, new Vector2(node->GetScaleX(), node->GetScaleY()));
        
        // Top
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(viewportSize.X, node->ScreenY), maskColor);

        // Left
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(node->ScreenX, viewportSize.Y), maskColor);

        // Right
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(node->ScreenX + node->GetWidth() * nodeScale.X, 0.0f), new Vector2(viewportSize.X, viewportSize.Y), maskColor);

        // Bottom
        ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, node->ScreenY + node->GetHeight() * nodeScale.Y), new Vector2(viewportSize.X, viewportSize.Y), maskColor);

        // Border
        ImGui.GetBackgroundDrawList().AddRect(new Vector2(node->ScreenX, node->ScreenY) - Vector2.One, new Vector2(node->ScreenX + node->GetWidth() * nodeScale.X, node->ScreenY + node->GetHeight() * nodeScale.Y) + Vector2.One, borderColor);
    }
}