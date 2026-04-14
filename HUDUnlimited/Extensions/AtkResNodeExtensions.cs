using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDUnlimited.Classes;
using KamiToolKit.Extensions;

namespace HUDUnlimited.Extensions;

public static unsafe class AtkResNodeExtensions {
    extension(AtkResNode node) {
        public void DrawOutline(Vector4 color, Vector4 focusColor, bool isFocused) {
            var position = node.ScreenPosition;
            var size = node.Size * DrawHelpers.GetNodeScale(&node, node.Scale);
            
            ImGui.GetForegroundDrawList().AddRect(
                position,
                position + size,
                ImGui.GetColorU32(color),
                5.0f,
                ImDrawFlags.RoundCornersAll,
                4.0f
            );

            if (isFocused) {
                ImGui.GetForegroundDrawList().AddRect(
                    position,
                    position + size,
                    ImGui.GetColorU32(focusColor),
                    5.0f,
                    ImDrawFlags.RoundCornersAll,
                    2.0f
                );
            }
        }

        public void DrawBorder(Vector4 color, float thickness) {
            var position = node.ScreenPosition;
            var size = node.Size * DrawHelpers.GetNodeScale(&node, node.Scale);
            
            ImGui.GetForegroundDrawList().AddRect(
                position,
                position + size,
                ImGui.GetColorU32(color),
                0,
                ImDrawFlags.RoundCornersAll,
                thickness
            );
        }
    }
}
