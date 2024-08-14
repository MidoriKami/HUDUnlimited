using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using HUDUnlimited.Classes;
using ImGuiNET;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public class OverrideListWindow : Window {
    public OverrideListWindow() : base("Override Browser", new Vector2(800.0f, 600.0f)) {
    }

    protected override void DrawContents() {
        NodeOverride? removalOption = null;
        
        var addonGroups = System.Config.Overrides.GroupBy(config => config.AddonName);
        foreach (var group in addonGroups) {
            if (ImGui.CollapsingHeader(group.Key)) {
                using var indent = ImRaii.PushIndent();

                foreach (var item in group) {
                    if (ImGuiComponents.IconButton($"delete##{item.NodePath}", FontAwesomeIcon.Trash)) {
                        removalOption = item;
                    }
                    
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(item.NodePath);
                }
            }
        }

        if (removalOption is not null) {
            System.Config.Overrides.Remove(removalOption);
        }
    }
}