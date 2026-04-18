using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDUnlimited.Extensions;
using KamiToolKit.Extensions;

namespace HUDUnlimited.WindowParts;

public unsafe class AddonSelect {
    private string filter = string.Empty;

    public required Action<Pointer<AtkUnitBase>> OnAddonSelected { get; init; }

    private ColorHelpers.HsvaColor baseColor = new(0.0f, 1.0f, 1.0f, 1.0f);

    public void Draw() {
        DrawAddonFilter();
        DrawAddonSelect();
    }

    private void DrawAddonFilter() {
        var filterChildHeight = 28.0f * ImGuiHelpers.GlobalScale;

        using var filterChild = ImRaii.Child("filter_child", new Vector2(ImGui.GetContentRegionAvail().X, filterChildHeight));
        if (!filterChild.Success) return;
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##filter", "Filter . . .", ref filter, 64, ImGuiInputTextFlags.AutoSelectAll);
    }

    private void DrawAddonSelect() {
        using var addonListBox = ImRaii.ListBox("##AddonSelect", ImGui.GetContentRegionAvail() - ImGuiHelpers.ScaledVector2(0.0f, 32.0f));
        if (!addonListBox) return;

        baseColor = new ColorHelpers.HsvaColor(0.0f, 1.0f, 1.0f, 1.0f);
        ImGuiClip.ClippedDraw(Addons, DrawAddon, ImGui.GetTextLineHeight());
    }
    
    private void DrawAddon(Pointer<AtkUnitBase> pointer) {
        var addon = pointer.Value;

        var cursorPosition = ImGui.GetCursorPos();
        if (ImGui.Selectable($"##{pointer.GetHashCode()}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()))) {
            OnAddonSelected.Invoke(pointer);
        }

        var isHovered = ImGui.IsItemHovered();
        ImGui.SetCursorPos(cursorPosition);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 7.0f * ImGuiHelpers.GlobalScale);

        var addonColor = ColorHelpers.HsvToRgb(baseColor);

        if (addon->IsActuallyVisible) {
            using (Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            using (ImRaii.PushColor(ImGuiCol.Text, addonColor)) {
                ImGui.Text(FontAwesomeIcon.SquareFull.ToIconString());
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5.0f * ImGuiHelpers.GlobalScale);
        }

        var addonNameColor = !addon->IsActuallyVisible ? KnownColor.Gray.Vector() with { W = 0.66f } : KnownColor.LightGreen.Vector();
        
        using (ImRaii.PushColor(ImGuiCol.Text, addonNameColor)) {
            ImGui.Text(addon->NameString);
        }

        if (addon->IsActuallyVisible) {
            var outlineColor = isHovered ? KnownColor.White.Vector() : addonColor;
            baseColor.H += 0.07f;

            addon->DrawOutline(outlineColor, KnownColor.Black.Vector(), isHovered);
        }
    }

    private List<Pointer<AtkUnitBase>> Addons => RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries.ToArray()
        .Where(entry => entry.Value is not null && entry.Value->IsReady)
        .Where(entry => filter == string.Empty || entry.Value->NameString.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(entry => entry.Value->IsActuallyVisible)
        .ThenBy(entry => entry.Value->NameString)
        .ToList();
}
