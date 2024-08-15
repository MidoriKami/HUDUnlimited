using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace HUDUnlimited.Classes;

public static class DrawHelpers {
    public static bool DrawFlagOption(OverrideConfig? option, OverrideFlags flag) {
        var positionFlag = option?.Flags.HasFlag(flag) ?? false;
        
        if (ImGui.Checkbox($"##FlagOption{flag.ToString()}", ref positionFlag)) {
            if (option is not null) {
                if (positionFlag) {
                    option.Flags |= flag;
                }
                else {
                    option.Flags &= ~flag;
                }
            }

            return true;
        }

        return false;
    }
    
    public static unsafe Vector2 GetNodeScale(AtkResNode* node, Vector2 currentScale) {
        if (node->ParentNode is not null) {
            currentScale.X *= node->ParentNode->GetScaleX();
            currentScale.Y *= node->ParentNode->GetScaleY();

            return GetNodeScale(node->ParentNode, currentScale);
        }

        return currentScale;
    }
    
    public static bool DrawOptionHeader(string label, OverrideConfig? option, OverrideFlags flag) {
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        ImGuiHelpers.ScaledDummy(5.0f);
        
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text(label);
        
        ImGui.TableNextRow(); 
        ImGui.TableNextColumn();
        return DrawFlagOption(option, flag);
    }
}