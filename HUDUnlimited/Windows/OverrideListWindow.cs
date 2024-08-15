using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HUDUnlimited.Classes;
using ImGuiNET;
using KamiLib.Classes;
using KamiLib.Window;
using Microsoft.VisualBasic;

namespace HUDUnlimited.Windows;

public class OverrideListWindow : Window {
    public OverrideListWindow() : base("Override Browser", new Vector2(600.0f, 600.0f)) {
    }

    private string selectedAddon = string.Empty;
    private OverrideConfig? selectionConfigOption = null;
    
    protected override void DrawContents() {
        using var table = ImRaii.Table("option_viewer", 2, ImGuiTableFlags.Resizable);
        if (table) {
            ImGui.TableSetupColumn("##addon_selection", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##option_config", ImGuiTableColumnFlags.WidthStretch);
            
            ImGui.TableNextColumn();
            using (var selectionChild = ImRaii.Child("selection_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
                if (selectionChild) {
                    using var listBox = ImRaii.ListBox("##addon_listBox", ImGui.GetContentRegionAvail());
                    if (listBox) {
                        foreach (var addon in System.Config.Overrides.GroupBy(option => option.AddonName)) {
                            if (ImGui.Selectable(addon.Key, selectedAddon == addon.Key)) {
                                selectedAddon = addon.Key;
                                selectionConfigOption = null;
                            }
                        }
                    }
                }
            }

            ImGui.TableNextColumn();
            using (var configChild = ImRaii.Child("config_child", ImGui.GetContentRegionAvail()  - ImGui.GetStyle().FramePadding)) {
                if (configChild) {
                    if (selectedAddon != string.Empty) {
                        ImGuiTweaks.Header(selectedAddon, true);

                        using var tabBar = ImRaii.TabBar("browser_tab_bar");
                        if (tabBar) {
                            using (var overview = ImRaii.TabItem("Overview")) {
                                if (overview) {
                                    DrawAddonOverview();
                                }
                            }
                                    
                            using (var configuration = ImRaii.TabItem("Configuration")) {
                                if (configuration) {
                                    DrawConfigOverview();
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawAddonOverview() {
        if (selectedAddon == string.Empty) return;
        using var child = ImRaii.Child("addon_overview");
        if (!child) return;
    }

    private void DrawConfigOverview() {
        if (selectedAddon == string.Empty) return;
        using var tabChild = ImRaii.Child("override_config");
        if (!tabChild) return;

        var configChanged = false;

        foreach (var group in System.Config.Overrides.GroupBy(config => config.AddonName)) {
            if (group.Key != selectedAddon) continue;
            OverrideConfig? removeConfig = null;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            using (var combo = ImRaii.Combo("option_select_combo", selectionConfigOption?.NodePath ?? "None Selected")) {
                if (combo) {
                    foreach (var config in group) {
                        if (ImGui.Selectable(config.NodePath, selectionConfigOption == config)) {
                            selectionConfigOption = config;
                        }
                    }
                }
            }
            
            ImGuiHelpers.ScaledDummy(10.0f);

            if (selectionConfigOption is not null) {
                using var configChild = ImRaii.Child("config_child");
                if (configChild.Success) {
                    using (ImRaii.PushIndent()) {
                        using var id = ImRaii.PushId(selectionConfigOption.NodePath);

                        using var table = ImRaii.Table($"option_table_{selectionConfigOption.NodePath}", 2);
                        if (!table) continue;

                        ImGui.TableSetupColumn("##enableColumn", ImGuiTableColumnFlags.WidthFixed, 25.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("##option", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1.0f)) {
                            using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {

                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 3.0f * ImGuiHelpers.GlobalScale);
                                ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
                            }

                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                ImGui.SetTooltip("Enable Override");
                            }
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text("Position");

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        configChanged |= DrawHelpers.DrawFlagOption(selectionConfigOption, OverrideFlags.Position);

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.Position))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.DragFloat2("##Position", ref selectionConfigOption.Position);
                        }

                        configChanged |= DrawHelpers.DrawOptionHeader("Scale", selectionConfigOption, OverrideFlags.Scale);

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.Scale))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.DragFloat2("##Scale", ref selectionConfigOption.Scale, 0.01f);
                        }

                        configChanged |= DrawHelpers.DrawOptionHeader("Color", selectionConfigOption, OverrideFlags.Color);

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.Color))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.ColorEdit4("##Color", ref selectionConfigOption.Color, ImGuiColorEditFlags.AlphaPreviewHalf);
                        }

                        configChanged |= DrawHelpers.DrawOptionHeader("Add Color", selectionConfigOption, OverrideFlags.AddColor);

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.AddColor))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.ColorEdit3("##AddColor", ref selectionConfigOption.AddColor);
                        }

                        configChanged |= DrawHelpers.DrawOptionHeader("Multiply Color", selectionConfigOption, OverrideFlags.MultiplyColor);

                        ImGui.TableNextColumn();

                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.MultiplyColor))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.ColorEdit3("##MultiplyColor", ref selectionConfigOption.MultiplyColor);
                        }

                        configChanged |= DrawHelpers.DrawOptionHeader("Visibility", selectionConfigOption, OverrideFlags.Visibility);

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!selectionConfigOption.Flags.HasFlag(OverrideFlags.Visibility))) {
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            configChanged |= ImGui.Checkbox("##Visibility", ref selectionConfigOption.Visible);
                        }

                    }
                    
                    ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 28.0f * ImGuiHelpers.GlobalScale);
                    using (var deleteChild = ImRaii.Child("delete_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
                        
                        if (deleteChild) {
                            using var disabled = ImRaii.Disabled(!(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl));

                            using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetContentRegionAvail())) {
                                    removeConfig = selectionConfigOption;
                                }
                            }

                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1.0f);
                                ImGui.SetTooltip("Hold Control + Shift to delete");
                            }
                        }
                    }
                    
                    if (removeConfig is not null) {
                        System.Config.Overrides.Remove(removeConfig);
                        selectionConfigOption = null;
                        System.Config.Save();
                    }
                }
            }
        }

        if (configChanged) {
            System.Config.Save();
        }
    }
    
    // foreach (var config in group) {
    //             }
}