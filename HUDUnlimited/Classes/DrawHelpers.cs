using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Extensions;

namespace HUDUnlimited.Classes;

public static class DrawHelpers {
    public static unsafe Vector2 GetNodeScale(AtkResNode* node, Vector2 currentScale) {
        if (node is null) return currentScale;
        
        if (node->ParentNode is not null) {
            currentScale *= node->Scale;

            return GetNodeScale(node->ParentNode, currentScale);
        }

        return currentScale;
    }
}
