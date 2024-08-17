using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HUDUnlimited.Classes;
using ImGuiNET;
using KamiLib.Classes;
using KamiLib.Window;

namespace HUDUnlimited.Windows;

public class OverrideListWindow() : Window("Override Browser", new Vector2(600.0f, 600.0f)) {

    private string selectedAddon = string.Empty;
    private OverrideConfig? selectionConfigOption;
    private List<OverrideConfig> selectedConfigs = []; 
    
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        IncludeFields = true,
    };
    
    protected override void DrawContents() {
        using var table = ImRaii.Table("option_viewer", 2, ImGuiTableFlags.Resizable);
        if (table) {
            ImGui.TableSetupColumn("##addon_selection", ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##option_config", ImGuiTableColumnFlags.WidthStretch);
            
            ImGui.TableNextColumn();
            using (var selectionChild = ImRaii.Child("selection_child",  new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().FramePadding.Y - 28.0f * ImGuiHelpers.GlobalScale))) {
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

            using (var importChild = ImRaii.Child("import_child", ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding)) {
                if (importChild) {
                    using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                        if (ImGui.Button(FontAwesomeIcon.FileImport.ToIconString(), ImGui.GetContentRegionAvail())) {
                            try {
                                var decodedString = Convert.FromBase64String(ImGui.GetClipboardText());
                                var uncompressed = Util.DecompressString(decodedString);

                                if (uncompressed.IsNullOrEmpty()) {
                                    Service.NotificationManager.AddNotification(new Notification {
                                        Type = NotificationType.Error, Content = "Unable to Import Presets",
                                    });
                                }

                                if (JsonSerializer.Deserialize<List<OverrideConfig>>(uncompressed, SerializerOptions) is { } importedData) {
                                    var addedCount = 0;
                                    foreach (var importedPreset in importedData) {
                                        if (System.Config.Overrides.All(existingOption => existingOption.NodePath != importedPreset.NodePath)) {
                                            System.Config.Overrides.Add(importedPreset);
                                            addedCount++;
                                        }
                                    }

                                    Service.NotificationManager.AddNotification(new Notification {
                                        Type = NotificationType.Info, Content = $"Imported {addedCount} new presets",
                                    });
                                }
                            }
                            catch (Exception e) {
                                Service.NotificationManager.AddNotification(new Notification {
                                    Type = NotificationType.Error, 
                                    Content = "Error reading data from clipboard, try copying preset again.",
                                    Minimized = false,
                                });
                                Service.PluginLog.Error(e, "Error parsing preset from clipboards");
                            }
                        }
                    }
                        
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Import Configs from Clipboard");
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

        using var tabChild = ImRaii.Child("overview_tab_child");
        if (tabChild) {
            using (var selectionChild = ImRaii.Child("selection_child", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 25.0f * ImGuiHelpers.GlobalScale) - ImGui.GetStyle().FramePadding)) {
                if (selectionChild) {
                    using var selectionList = ImRaii.ListBox("##option_multi_select", ImGui.GetContentRegionAvail());
                    if (selectionList) {
                        foreach (var option in System.Config.Overrides.Where(configOption => configOption.AddonName == selectedAddon)) {
                            if (ImGui.Selectable(option.NodeName, selectedConfigs.Contains(option))) {
                                if (!selectedConfigs.Remove(option)) {
                                    selectedConfigs.Add(option);
                                }
                            }
                        }
                    }
                }
            }

            using (var optionsChild = ImRaii.Child("options_child", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                if (optionsChild) {
                    using var table = ImRaii.Table("buttons_table", 4, ImGuiTableFlags.SizingStretchProp);
                    if (table) {
                        ImGui.TableSetupColumn("##SelectAll", ImGuiTableColumnFlags.WidthStretch, 3);
                        ImGui.TableSetupColumn("##DeselectAll", ImGuiTableColumnFlags.WidthStretch, 3);
                        ImGui.TableSetupColumn("##Export", ImGuiTableColumnFlags.WidthStretch, 1);
                        ImGui.TableSetupColumn("##Delete", ImGuiTableColumnFlags.WidthStretch, 1);
                    
                        ImGui.TableNextColumn();
                        if (ImGui.Button("Select All", ImGui.GetContentRegionAvail())) {
                            selectedConfigs.Clear();
                            foreach (var option in System.Config.Overrides.Where(configOption => configOption.AddonName == selectedAddon)) {
                                selectedConfigs.Add(option);
                            }
                        }
                
                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(selectedConfigs.Count == 0)) {
                            if (ImGui.Button("Deselect All", ImGui.GetContentRegionAvail())) {
                                selectedConfigs.Clear();
                            }
                        }
                
                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(selectedConfigs.Count == 0)) {
                            using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                                if (ImGui.Button(FontAwesomeIcon.FileExport.ToIconString(), ImGui.GetContentRegionAvail())) {
                                    var data = selectedConfigs;
                                    var jsonString = JsonSerializer.Serialize(data, SerializerOptions);
                                    var compressed = Util.CompressString(jsonString);
                                    ImGui.SetClipboardText(Convert.ToBase64String(compressed));

                                    Service.NotificationManager.AddNotification(new Notification {
                                        Type = NotificationType.Info, Content = $"Exported {data.Count} presets to clipboard.",
                                    });
                                }
                            }
                            
                            if (ImGui.IsItemHovered()) {
                                ImGui.SetTooltip("Export Configs to Clipboard");
                            }
                        }

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(!(ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift) || selectedConfigs.Count == 0)) {
                            using (Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetContentRegionAvail())) {
                                    System.Config.Overrides.RemoveAll(option => selectedConfigs.Contains(option));
                                    System.Config.Save();

                                    if (!System.Config.Overrides.Any(configOption => configOption.AddonName == selectedAddon)) {
                                        selectedAddon = string.Empty;
                                        selectionConfigOption = null;
                                    }
                                }
                            }

                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1.0f)) {
                                    ImGui.SetTooltip("Hold Control + Shift to delete selected options");
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawConfigOverview() {
        if (selectedAddon == string.Empty) return;
        using var tabChild = ImRaii.Child("override_config");
        if (!tabChild) return;

        foreach (var group in System.Config.Overrides.GroupBy(config => config.AddonName)) {
            if (group.Key != selectedAddon) continue;
            OverrideConfig? removeConfig = null;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            using (var combo = ImRaii.Combo("option_select_combo", selectionConfigOption?.NodeName ?? "None Selected")) {
                if (combo) {
                    foreach (var config in group) {
                        if (ImGui.Selectable(config.NodeName, selectionConfigOption == config)) {
                            selectionConfigOption = config;
                        }
                    }
                }
            }
            
            ImGuiHelpers.ScaledDummy(10.0f);

            if (selectionConfigOption is not null) {
                using var configChild = ImRaii.Child("config_child");
                if (configChild.Success) {
                    selectionConfigOption.DrawConfig();
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
                    if (System.Config.Overrides.Count(options => options.AddonName == selectedAddon) is 1) {
                        selectedAddon = string.Empty;
                    }
                    System.Config.Save();
                }
            }
        }
    }
}