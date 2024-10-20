using System.Net.Security;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using KamiLib.Classes;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public class InfoWindow() : Window("HUDUnlimited Information Window", new Vector2(600.0f, 750.0f), true) {
    protected override void DrawContents() {
        ImGuiTweaks.Header("What is HUDUnlimited");
        using (ImRaii.PushIndent()) {
            ImGui.TextWrapped("HUDUnlimited allows you to edit various properties of any individual UI element.");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("Giving you full control over how your UI looks.");
        }
        
        ImGuiTweaks.Header("I did something I didn't like, how do I undo it?");
        using (ImRaii.PushIndent()) {
            ImGui.TextWrapped("If you made a change that you don't like, disable the overrides for the relevant elements, and then cause that element to be reloaded.");

            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("If you are changing parts of a window, simply close and reopen that window and that will trigger the effected elements to be reloaded.");

            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("If you are changing a persistent part of the UI then do one of the following:");

            ImGuiHelpers.ScaledDummy(3.0f);
            using (ImRaii.PushIndent()) {
                ImGui.Text("1. Logout and then back in");
                ImGui.Text("2. Visit the Aestetician");
                ImGui.Text("3. Participate in Lord of Verminion");
            }

            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("These actions will cause the entirety of the game UI to be rebuilt, thus restoring everything back to their original values.");
        }
        
        ImGuiTweaks.Header("How do I just move something?");
        using (ImRaii.PushIndent()) {
            ImGui.TextWrapped("The game UI is a complex web of UI \"Nodes\" these nodes are chained and linked together to create our UI's.");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("The game generally groups things together into \"Components\" if the developers intend to reuse that element again.");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("Finding a component that contains what you want to change is generally a good place to start.");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("An easy way to see if you have the right part selected is to enable overrides for that part and then toggle the visibility.If the thing you wanted to change disappears, then that's the right part! If it doesn't disappear, delete your override, and try again!");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("If the thing you wanted to change disappears, then that's the right part!");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("If it doesn't disappear, delete your override, and try again!");
            
            ImGuiHelpers.ScaledDummy(3.0f);
            ImGui.TextWrapped("It'll be a lot of trial and error, good luck!");
        }
    }
}