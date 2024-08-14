using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public class OverrideListWindow : Window {
    public OverrideListWindow() : base("Override Browser", new Vector2(800.0f, 600.0f)) {
    }

    protected override void DrawContents() {
        // using var table = ImRaii.Table("option")
        //
        // var addonGroups = System.Config.Overrides.GroupBy(config => config.AddonName);
        // foreach (var group in addonGroups) {
        //     
        // }
    }
}