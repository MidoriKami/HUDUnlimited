using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDUnlimited.Classes;

public static class DrawHelpers {
    public static unsafe Vector2 GetNodeScale(AtkResNode* node, Vector2 currentScale) {
        if (node->ParentNode is not null) {
            currentScale.X *= node->ParentNode->GetScaleX();
            currentScale.Y *= node->ParentNode->GetScaleY();

            return GetNodeScale(node->ParentNode, currentScale);
        }

        return currentScale;
    }
    

}