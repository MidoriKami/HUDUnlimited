using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDUnlimited.Classes;
using KamiToolKit.Extensions;

namespace HUDUnlimited.Extensions;

public static unsafe class AtkResNodeExtensions {
    extension(AtkResNode node) {
        public void DrawOutline(Vector4 color, Vector4 focusColor, bool isFocused) {
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(&node);
            if (addon is null) return;
            
            var scale = DrawHelpers.GetNodeScale(&node, node.Scale) * addon->Scale;
            var position = node.ScreenPosition;
            var size = node.Size * scale;
            
            var drawList = isFocused ? ImGui.GetForegroundDrawList() : ImGui.GetBackgroundDrawList();
            
            drawList.AddRect(
                position,
                position + size,
                ImGui.GetColorU32(color),
                5.0f,
                ImDrawFlags.RoundCornersAll,
                System.Config.LineThickness
            );

            if (isFocused) {
                drawList.AddRect(
                    position,
                    position + size,
                    ImGui.GetColorU32(focusColor),
                    5.0f,
                    ImDrawFlags.RoundCornersAll,
                    Math.Max(System.Config.LineThickness - 1.0f, 1.0f)
                );
            }
        }

        public void DrawBorder(Vector4 color) {
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(&node);
            if (addon is null) return;

            var scale = DrawHelpers.GetNodeScale(&node, node.Scale) * addon->Scale;
            var position = node.ScreenPosition;
            var size = node.Size * scale;
            
            ImGui.GetForegroundDrawList().AddRect(
                position,
                position + size,
                ImGui.GetColorU32(color),
                0,
                ImDrawFlags.RoundCornersAll,
                System.Config.LineThickness
            );
        }
    }
}
