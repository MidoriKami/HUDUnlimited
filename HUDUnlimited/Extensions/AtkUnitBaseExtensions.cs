using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Extensions;

namespace HUDUnlimited.Extensions;

public static class AtkUnitBaseExtensions {
    extension(AtkUnitBase addon) {
        public void DrawOutline(Vector4 color, Vector4 focusColor, bool isFocused) {
            var position = addon.Position;
            var size = addon.Size * addon.Scale;
            
            var drawList = isFocused ? ImGui.GetForegroundDrawList() : ImGui.GetBackgroundDrawList();
            
            drawList.AddRect(
                position, 
                position + size , 
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
    }
}
