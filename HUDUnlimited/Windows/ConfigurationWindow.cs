using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
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

    public ConfigurationWindow() : base("HUDUnlimited Configuration Window", new Vector2(900.0f, 500.0f)) {
        TitleBarButtons.Add(new TitleBarButton {
            Click = _ => System.OverrideListWindow.UnCollapseOrToggle(),
            Icon = FontAwesomeIcon.Cog,
            ShowTooltip = () => ImGui.SetTooltip("Open Preset Browser"),
            IconOffset = new Vector2(2.0f, 1.0f),
        });
        
        TitleBarButtons.Add(new TitleBarButton {
            Click = _ => System.InfoWindow.UnCollapseOrToggle(),
            Icon = FontAwesomeIcon.InfoCircle,
            ShowTooltip = () => ImGui.SetTooltip("Open Plugin Help/Info"),
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
        DrawAddonNameFilter();
        DrawAddonSelectionChild();

        ImGui.TableNextColumn();
        DrawNodeExtrasTable();
        DrawNodeSelectionChild();
        
        ImGui.TableNextColumn();
        DrawNodeConfigurationChild();
    }

    private void DrawAddonNameFilter() {
        using var filterChild = ImRaii.Child("filter_child", new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale));
        if (!filterChild) return;
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##filter", "filter...", ref filter, 64, ImGuiInputTextFlags.AutoSelectAll);
    }

    private void DrawAddonSelectionChild() {
        using var addonChild = ImRaii.Child("addon_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding);
        if (!addonChild) return;
        
        DrawAddonSelection();
    }
    
    private void DrawNodeExtrasTable() {
        using var nodeExtrasTable = ImRaii.Table("node_extras_table", 2);
        if (!nodeExtrasTable) return;
        
        ImGui.TableSetupColumn("##typeFilter", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##locatorButton", ImGuiTableColumnFlags.WidthFixed, 32.0f * ImGuiHelpers.GlobalScale);

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        DrawNodeTypeSelection();

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

    private void DrawNodeTypeSelection() {
        using var typeCombo = ImRaii.Combo("##nodeTypeFilter", typeFilter.ToString(), ImGuiComboFlags.HeightLarge);
        if (!typeCombo) return;
        
        foreach (var enumValue in Enum.GetValues<ExtendedNodeType>()) {
            if (ImGui.Selectable(enumValue.ToString(), typeFilter == enumValue)) {
                typeFilter = enumValue;
            }
        }
    }

    private void DrawNodeSelectionChild() {
        using var nodeChild = ImRaii.Child("node_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding);
        if (!nodeChild) return;
        
        DrawNodeSelection();
    }

    private void DrawNodeConfigurationChild() {
        using var nodeConfigChild = ImRaii.Child("node_config_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding);
        if (!nodeConfigChild) return;
        
        DrawNodeConfiguration();
    }

    private void DrawAddonSelection() {
        using var frameBg = ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBg] with { W = 0.10f });
        using var scrollBarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 0.0f);
             
        var extraButtonSize = new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale);
        var listBoxSize = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing - extraButtonSize;
             
        DrawAddonSelectionList(listBoxSize);
        DrawAddonSelectionShowHideButton();
    }

    private void DrawAddonSelectionList(Vector2 listBoxSize) {
        using var listBox = ImRaii.ListBox("##addonSelectionListBox", listBoxSize);
        if (!listBox) return;
        
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

    private static void DrawAddonSelectionShowHideButton() {
        using var selectionListButtonChild = ImRaii.Child("addon_selection_list_button", ImGui.GetContentRegionAvail());
        if (!selectionListButtonChild) return;
        var text = System.Config.HideInactiveAddons ? "Show Inactive" : "Hide Inactive";
     
        if (ImGui.Button(text, ImGui.GetContentRegionAvail())) {
            System.Config.HideInactiveAddons = !System.Config.HideInactiveAddons;
            System.Config.Save();
        }
    }

    private void DrawNodeSelection() {
        using var frameBg = ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBg] with { W = 0.10f });

        var extraButtonSize = new Vector2(ImGui.GetContentRegionAvail().X, 28.0f * ImGuiHelpers.GlobalScale);
        var listBoxSize = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemInnerSpacing - extraButtonSize;
             
        DrawNodeTypeListBox(listBoxSize);
        DrawNodeSelectionShowHideButton();
    }

    private void DrawNodeTypeListBox(Vector2 listBoxSize) {
        using var listBox = ImRaii.ListBox("##nodeSelectionListBox", listBoxSize);
        if (!listBox) return;
        
        using var headerHoverColor = ImRaii.PushColor(ImGuiCol.HeaderHovered, ImGui.GetStyle().Colors[(int) ImGuiCol.HeaderHovered] with { W = 0.1f });
        using var textSelectedColor = ImRaii.PushColor(ImGuiCol.Header, ImGui.GetStyle().Colors[(int) ImGuiCol.Header] with { W = 0.1f });
                
        if (selectedAddon is not null) {
            DrawNodeRecursively(ref selectedAddon->UldManager, selectedAddon->NameString);
        }
    }

    private static void DrawNodeSelectionShowHideButton() {
        using var selectionListButtonChild = ImRaii.Child("node_selection_list_button", ImGui.GetContentRegionAvail());
        if (!selectionListButtonChild) return;
        
        var text = System.Config.HideInactiveNodes ? "Show Inactive" : "Hide Inactive";
     
        if (ImGui.Button(text, ImGui.GetContentRegionAvail())) {
            System.Config.HideInactiveNodes = !System.Config.HideInactiveNodes;
            System.Config.Save();
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
        DrawEnableDisableChild(option, buttonText);
        
        if (locateNode) {
            HighlightNode(selectedNode);
        }
    }

    private void DrawEnableDisableChild(OverrideConfig? option, string buttonText) {
        using var buttonChild = ImRaii.Child("enable_disable_button", ImGui.GetContentRegionAvail());
        if (!buttonChild) return;

        DrawCopyConfigButton(option);
        ImGui.SameLine();
                
        DrawPasteConfigButton(option);
        ImGui.SameLine();
                
        if (ImGui.Button(buttonText, new Vector2(ImGui.GetContentRegionMax().X * 3.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {

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
                    ProxyParentName = GetProxyNameForSelectedAddon(),
                };
                        
                System.Config.Overrides.Add(newOption);
                System.AddonController.EnableOverride(newOption);
            }
            else {
                if (option.OverrideEnabled) {
                    option.OverrideEnabled = false;
                    System.AddonController.DisableOverride(option);
                }
                else {
                    option.OverrideEnabled = true;
                    System.AddonController.EnableOverride(option);
                }
            }
            System.Config.Save();
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

    private void DrawNodeHeader() {
        if (selectedNode is null) return;
        
        ImGui.AlignTextToFramePadding();
        ImGuiHelpers.CenteredText(selectedNode->Type.ToString());
        
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = 0.50f })) {
            ImGuiHelpers.CenteredText(selectedNodePath);
        }
    }
    
    private static void DrawCopyConfigButton(OverrideConfig? option) {
        using var disabled = ImRaii.Disabled(option is null);
        
        if (ImGui.Button("Copy", new Vector2(ImGui.GetContentRegionMax().X * 1.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {
            var jsonString = JsonSerializer.Serialize(option, JsonSettings.SerializerOptions);
            var compressed = Util.CompressString(jsonString);
            ImGui.SetClipboardText(Convert.ToBase64String(compressed));
        }
            
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Copy Configuration");
        }
        ImGui.SameLine();
    }

    private void DrawPasteConfigButton(OverrideConfig? option) {
        if (ImGui.Button("Paste", new Vector2(ImGui.GetContentRegionMax().X * 1.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {
            var decodedString = Convert.FromBase64String(ImGui.GetClipboardText());
            var uncompressed = Util.DecompressString(decodedString);

            if (uncompressed.IsNullOrEmpty()) {
                Service.NotificationManager.AddNotification(new Notification {
                    Type = NotificationType.Error, Content = "Unable to Paste Configuration",
                });
            }

            if (JsonSerializer.Deserialize<OverrideConfig>(uncompressed, JsonSettings.SerializerOptions) is { } pastedConfiguration) {
                
                // Overwrite the node path with the node we are trying to actually update.
                pastedConfiguration.NodePath = selectedNodePath;
                
                // If we don't have an option already, make a new one with the new data
                if (System.Config.Overrides.All(existingOption => existingOption.NodePath != pastedConfiguration.NodePath)) {
                    System.Config.Overrides.Add(pastedConfiguration);
                    System.AddonController.EnableOverride(pastedConfiguration);
                }

                // If we do already have an option, set the values
                else {
                    pastedConfiguration.CopyTo(option);
                }

                Service.NotificationManager.AddNotification(new Notification {
                    Type = NotificationType.Info, Content = $"Pasted Configuration: {pastedConfiguration.NodePath}",
                });
            
                System.Config.Save();
            }
        }

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Paste Configuration");
        }
    }

    private void DrawNodeOptions() {
        using var child = ImRaii.Child("node_options_child", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 28.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        
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
        if (uldManager.LoadedState is not AtkLoadState.Loaded) return;
        
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
                ExtendedNodeType.Component when node.Value->GetNodeType() is NodeType.Component => true,
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
                if (option is { CustomName: not "" }) {
                    ImGui.TextColored(color, option.CustomName);  
                }
                else {
                    ImGui.TextColored(color, isComponentNode ? "Component" : node.Value->Type.ToString());  
                }

                if (option is not null) {
                    using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                        var symbolSize = ImGui.CalcTextSize(FontAwesomeIcon.StarHalfAlt.ToIconString());
                        ImGui.SameLine(ImGui.GetContentRegionMax().X - symbolSize.X);
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

        var viewportOffset = ImGui.GetMainViewport().Pos;
        var viewportSize = ImGui.GetMainViewport().Size;
        var maskColor = ImGui.GetColorU32(KnownColor.Gray.Vector() with { W = 0.80f });
        var borderColor = ImGui.GetColorU32(KnownColor.Red.Vector());

        var nodeScale = DrawHelpers.GetNodeScale(node, new Vector2(node->GetScaleX(), node->GetScaleY()));
        
        // Top
        ImGui.GetBackgroundDrawList()
            .AddRectFilled(new Vector2(0.0f, 0.0f) + viewportOffset, viewportSize with { Y = node->ScreenY } + viewportOffset, maskColor);

        // Left
        ImGui.GetBackgroundDrawList()
            .AddRectFilled(new Vector2(0.0f, 0.0f) + viewportOffset, viewportSize with { X = node->ScreenX } + viewportOffset, maskColor);

        // Right
        ImGui.GetBackgroundDrawList()
            .AddRectFilled(new Vector2(node->ScreenX + node->GetWidth() * nodeScale.X, 0.0f) + viewportOffset, new Vector2(viewportSize.X, viewportSize.Y) + viewportOffset, maskColor);

        // Bottom
        ImGui.GetBackgroundDrawList()
            .AddRectFilled(new Vector2(0.0f, node->ScreenY + node->GetHeight() * nodeScale.Y) + viewportOffset, new Vector2(viewportSize.X, viewportSize.Y) + viewportOffset, maskColor);

        // Border
        ImGui.GetBackgroundDrawList()
            .AddRect(new Vector2(node->ScreenX, node->ScreenY) - Vector2.One + viewportOffset, new Vector2(node->ScreenX + node->GetWidth() * nodeScale.X, node->ScreenY + node->GetHeight() * nodeScale.Y) + Vector2.One + viewportOffset, borderColor);
    }

    private string? GetProxyNameForSelectedAddon() {
        if (selectedAddon is null) return null;
        if (selectedAddon->HostId is not 0) {
            var proxyAddon = RaptureAtkUnitManager.Instance()->GetAddonById(selectedAddon->HostId);
            if (proxyAddon is not null) {
                return proxyAddon->NameString;
            }
        }

        return null;
    }
}