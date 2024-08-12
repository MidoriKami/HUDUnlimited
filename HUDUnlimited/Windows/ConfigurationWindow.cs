using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using ImGuiNET;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public unsafe class ConfigurationWindow() : SelectionListWindow<Pointer<AtkUnitBase>>("HUDUnlimited", new Vector2(800.0f, 600.0f), true) {

    protected override List<Pointer<AtkUnitBase>> Options => System.AddonListController.Addons;

    protected override float SelectionListWidth { get; set; } = 200.0f;

    protected override bool AllowChildScrollbar => true;

    protected override float SelectionItemHeight => ImGui.GetTextLineHeight();

    private AtkResNode* selectedNode = null;
    private bool LocateNode;
    private bool ShowOnHover;
    
    protected override void DrawListOption(Pointer<AtkUnitBase> option) {
        var color = option.Value->IsVisible ? KnownColor.LightGreen.Vector() : KnownColor.Gray.Vector();
        
        ImGui.TextColored(color, option.Value->NameString);
    }

    protected override void DrawSelectedOption(Pointer<AtkUnitBase> option) {
        using (var child = ImRaii.Child("toolbar", new Vector2(ImGui.GetContentRegionAvail().X, 50.0f * ImGuiHelpers.GlobalScale))) {
            if (child) {
                ImGui.Checkbox("locate Node", ref LocateNode);
                
                ImGui.SameLine();
                ImGui.Checkbox("OnHover", ref ShowOnHover);
            }
        }

        using (var nodeChild = ImRaii.Child("node_child", ImGui.GetContentRegionAvail())) {
            if (nodeChild) {
                DrawNodeRecursively(ref option.Value->UldManager, option.Value->NameString);
            }
        }
    }

    private void DrawNodeRecursively(ref AtkUldManager uldManager, string currentPath) {
        var nodeSpan = new Span<Pointer<AtkResNode>>(uldManager.NodeList, uldManager.NodeListCount);

        foreach (var node in nodeSpan) {
            if (node.Value is null) continue;
            
            if ((uint)node.Value->Type > 1000) {
                var componentNode = (AtkComponentNode*) node.Value;

                DrawNodeRecursively(ref componentNode->Component->UldManager, $"{currentPath}/{node.Value->NodeId}");
            }
            else {
                DrawNodeData(node.Value, $"{currentPath}/{node.Value->NodeId}");
            }
        }
    }

    private void DrawNodeData(AtkResNode* node, string currentPath) {
        var isSelected = node == selectedNode;

        using (var isHiddenStyle = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = 0.50f }, !node->IsVisible())) {
            if (ImGui.Selectable($"{node->Type}##{currentPath}", isSelected)) {
                selectedNode = node;
            }
        }

        var showOnHover = ImGui.IsItemHovered() && ShowOnHover && LocateNode;
        var showOnSelect = isSelected && LocateNode;

        if (showOnHover || showOnSelect) {
            var viewportSize = ImGui.GetMainViewport().Size;
            
            // Top
            ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(viewportSize.X, node->ScreenY), ImGui.GetColorU32(KnownColor.Gray.Vector()));

            // Left
            ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, 0.0f), new Vector2(node->ScreenX, viewportSize.Y), ImGui.GetColorU32(KnownColor.Gray.Vector()));

            // Right
            ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(node->ScreenX + node->Width, 0.0f), new Vector2(viewportSize.X, viewportSize.Y), ImGui.GetColorU32(KnownColor.Gray.Vector()));

            // Bottom
            ImGui.GetBackgroundDrawList().AddRectFilled(new Vector2(0.0f, node->ScreenY + node->Height), new Vector2(viewportSize.X, viewportSize.Y), ImGui.GetColorU32(KnownColor.Gray.Vector()));
        }

        if (isSelected) {
            using var indent = ImRaii.PushIndent();
            
            ImGui.Text(node->ScreenX.ToString(CultureInfo.InvariantCulture));
            ImGui.Text(node->ScreenY.ToString(CultureInfo.InvariantCulture));
        }
    }
}