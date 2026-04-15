using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDUnlimited.Enums;
using HUDUnlimited.Extensions;
using KamiToolKit.Extensions;

namespace HUDUnlimited.WindowParts;

public unsafe class ComponentSelect {

    public required Action<Pointer<AtkResNode>> OnNodeSelected { get; init; }
    
    private ColorHelpers.HsvaColor baseColor = new(0.0f, 1.0f, 1.0f, 1.0f);

    private ExtendedNodeType typeFilter;
    private bool showColorOutlines;
    private AtkResNode* selectedNode;
    private string nodePath = string.Empty;
    
    public void Draw(AtkUldManager* currentNodeManager, AtkResNode* node, string currentPath) {
        showColorOutlines = node is null || node->GetNodeType() is NodeType.Component;
        selectedNode = node;
        nodePath = currentPath;
        
        DrawNodeTypeFilter();
        DrawNodeSelect(currentNodeManager);
    }

    private void DrawNodeTypeFilter() {
        var filterChildHeight = 28.0f * ImGuiHelpers.GlobalScale;

        using var filterChild = ImRaii.Child("filter_child", new Vector2(ImGui.GetContentRegionAvail().X, filterChildHeight));
        if (!filterChild.Success) return;
                
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);

        using var typeCombo = ImRaii.Combo("##nodeTypeFilter", typeFilter is 0 ? "None" : typeFilter.ToString(), ImGuiComboFlags.HeightLarge);
        if (!typeCombo.Success) return;

        foreach (var enumValue in Enum.GetValues<ExtendedNodeType>()) {
            if (ImGui.Selectable(enumValue.ToString(), typeFilter == enumValue)) {
                typeFilter = enumValue;
            }
        }
    }

    private void DrawNodeSelect(AtkUldManager* currentNodeManager) {
        using var addonListBox = ImRaii.ListBox("##ComponentSelect", ImGui.GetContentRegionAvail());
        if (!addonListBox) return;

        var nodes = currentNodeManager->Nodes.ToArray()
            .Where(node => node.Value is not null)
            .Where(IsNodePermitted)
            .ToList();
        
        baseColor = new ColorHelpers.HsvaColor(0.0f, 1.0f, 1.0f, 1.0f);
        ImGuiClip.ClippedDraw(nodes, DrawNodeManager, ImGui.GetTextLineHeight());
    }

    private bool IsNodePermitted(Pointer<AtkResNode> pointer) {
        if (typeFilter is ExtendedNodeType.None) return true;
        
        var nodeType = pointer.Value->GetNodeType();
        
        return typeFilter switch {
            ExtendedNodeType.None => true,
            ExtendedNodeType.Res when nodeType is NodeType.Res => true,
            ExtendedNodeType.Image when nodeType is NodeType.Image => true,
            ExtendedNodeType.Text when nodeType is NodeType.Text => true,
            ExtendedNodeType.NineGrid when nodeType is NodeType.NineGrid => true,
            ExtendedNodeType.Counter when nodeType is NodeType.Counter => true,
            ExtendedNodeType.Collision when nodeType is NodeType.Collision => true,
            ExtendedNodeType.ClippingMask when nodeType is NodeType.ClippingMask => true,
            ExtendedNodeType.Component when nodeType is NodeType.Component => true,
            _ => false,
        };
    }

    private void DrawNodeManager(Pointer<AtkResNode> pointer) {
        var node = pointer.Value;
        using var id = ImRaii.PushId(node->NodeId.ToString());
        
        var cursorPosition = ImGui.GetCursorPos();
        if (ImGui.Selectable($"##{pointer.GetHashCode()}", pointer.Value == selectedNode, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()))) {
            OnNodeSelected.Invoke(pointer);
            typeFilter = ExtendedNodeType.None;
        }

        var isHovered = ImGui.IsItemHovered();
        ImGui.SetCursorPos(cursorPosition);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 7.0f * ImGuiHelpers.GlobalScale);

        var nodeColor = ColorHelpers.HsvToRgb(baseColor);

        if (node->IsActuallyVisible && showColorOutlines) {
            using (Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            using (ImRaii.PushColor(ImGuiCol.Text, nodeColor)) {
                ImGui.Text(FontAwesomeIcon.SquareFull.ToIconString());
            }
        }

        var nodeNameColor = !pointer.Value->IsActuallyVisible ? KnownColor.Gray.Vector() with { W = 0.66f } : KnownColor.LightGreen.Vector();

        using (ImRaii.PushColor(ImGuiCol.Text, nodeNameColor)) {
            ImGui.SameLine(40.0f * ImGuiHelpers.GlobalScale);
            ImGui.Text($"{node->NodeId}");

            ImGui.SameLine(125.0f * ImGuiHelpers.GlobalScale);

            var configData = System.Config.Overrides.FirstOrDefault(option => option.NodePath == $"{nodePath}/{node->NodeId}");
            if (configData is { CustomName: { Length: > 0 } customName }) {
                ImGui.Text(customName);
                
                using (Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                    var symbolSize = ImGui.CalcTextSize(FontAwesomeIcon.StarHalfAlt.ToIconString());
                    ImGui.SameLine(ImGui.GetContentRegionMax().X - symbolSize.X);
                    ImGui.Text(configData.OverrideEnabled ? FontAwesomeIcon.Star.ToIconString() : FontAwesomeIcon.StarHalfAlt.ToIconString());
                }
            }
            else {  
                ImGui.Text($"{node->GetNodeType()}");
            }
        }
        
        if (node->IsActuallyVisible && showColorOutlines) {
            var outlineColor = isHovered ? KnownColor.White.Vector() : nodeColor;
            baseColor.H += 0.07f;

            node->DrawOutline(outlineColor, KnownColor.Black.Vector(), isHovered);
        }
    }
}
