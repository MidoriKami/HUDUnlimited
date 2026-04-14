using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDUnlimited.Classes;
using HUDUnlimited.Extensions;

namespace HUDUnlimited.WindowParts;

public unsafe class NodeConfiguration {
    private string currentPath = string.Empty;
    private AtkResNode* currentNode = null;
    
    public void Draw(AtkResNode* node, string nodePath) {
        currentPath = nodePath;
        currentNode = node;

        if (node is null) {
            ImGuiHelpers.CenteredText("Select a node on the left");
            return;
        }
        
        var footerHeight = 30.0f * ImGuiHelpers.GlobalScale;
        
        using var child = ImRaii.Child("node_options_child", ImGui.GetContentRegionAvail());
        if (!child) return;
        
        DrawNodeHeader();
        
        using (var body = ImRaii.Child("Body", ImGui.GetContentRegionAvail() - new Vector2(0.0f, footerHeight))) {
            if (body.Success) {
                var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == nodePath);

                if (option is null || !option.OverrideEnabled) {
                    using (ImRaii.PushColor(ImGuiCol.Text, KnownColor.Orange.Vector())) {
                        ImGuiHelpers.ScaledDummy(10.0f);
                        ImGuiHelpers.CenteredText("Editing is currently Disabled");
                        ImGuiHelpers.ScaledDummy(10.0f);
                        ImGuiHelpers.CenteredText("Enable Overrides for this Node to configure");
                    }
                }

                // Text node features are newer than other features, if IsTextNode is null,
                // then we need to update the text node properties
                if (option is not null && option.IsTextNode is null) {
                    var isTextNode = node->GetNodeType() is NodeType.Text;
            
                    option.IsTextNode = isTextNode;
                    if (isTextNode) {
                        var textNode = (AtkTextNode*) node;
                        option.TextColor = textNode->TextColor.ToVector4();
                        option.TextOutlineColor = textNode->EdgeColor.ToVector4();
                        option.TextBackgroundColor = textNode->BackgroundColor.ToVector4();
                        option.FontSize = textNode->FontSize;
                        option.FontType = textNode->FontType;
                        option.AlignmentType = textNode->AlignmentType;
                        option.TextFlags = textNode->TextFlags;
                        System.Config.Save();
                    }
                }

                if (option is { OverrideEnabled: true }) {
                    option.DrawConfig();
                }
            }
        }

        using (var footer = ImRaii.Child("Footer", ImGui.GetContentRegionAvail())) {
            if (footer.Success) {
                DrawEnableDisableChild();
            }
        }
    }
    
        
    private void DrawNodeHeader() {
        ImGui.AlignTextToFramePadding();
        ImGuiHelpers.CenteredText(currentNode->GetNodeType().ToString());
        
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = 0.50f })) {
            ImGuiHelpers.CenteredText(currentPath);
        }
    }
    
    private void DrawEnableDisableChild() {
        var option = System.Config.Overrides.FirstOrDefault(option => option.NodePath == currentPath);
        var buttonText = option?.OverrideEnabled ?? false ? "Disable Overrides" : "Enable Overrides";
        
        using var buttonChild = ImRaii.Child("enable_disable_button", ImGui.GetContentRegionAvail());
        if (!buttonChild) return;

        DrawCopyConfigButton(option);
        ImGui.SameLine();
                
        DrawPasteConfigButton(option);
        ImGui.SameLine();
                
        if (ImGui.Button(buttonText, new Vector2(ImGui.GetContentRegionMax().X * 3.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {

            // Create and enable new option
            if (option is null) {
                var newOption = new OverrideConfig {
                    NodePath = currentPath,
                    OverrideEnabled = true,
                    Position = new Vector2(currentNode->GetXFloat(), currentNode->GetYFloat()),
                    Scale = new Vector2(currentNode->GetScaleX(), currentNode->GetScaleY()),
                    Color = currentNode->Color.ToVector4(),
                    AddColor = new Vector3(currentNode->AddRed, currentNode->AddGreen, currentNode->AddBlue) / 255.0f,
                    SubtractColor = Vector3.Zero,
                    MultiplyColor = new Vector3(currentNode->MultiplyRed, currentNode->MultiplyGreen, currentNode->MultiplyBlue) / 255.0f,
                    Visible = currentNode->IsVisible(),
                    ProxyParentName = GetProxyNameForSelectedAddon(),
                };

                if (currentNode is not null && currentNode->GetNodeType() is NodeType.Text) {
                    var textNode = (AtkTextNode*) currentNode;
                    
                    newOption.IsTextNode = true;
                    newOption.TextColor = textNode->TextColor.ToVector4();
                    newOption.TextOutlineColor = textNode->EdgeColor.ToVector4();
                    newOption.TextBackgroundColor = textNode->BackgroundColor.ToVector4();
                    newOption.FontSize = textNode->FontSize;
                    newOption.FontType = textNode->FontType;
                    newOption.AlignmentType = textNode->AlignmentType;
                    newOption.TextFlags = textNode->TextFlags;
                }
                        
                System.Config.Overrides.Add(newOption);
                System.AddonController.EnableOverride(newOption);
            }
            else {
                if (option.OverrideEnabled) {
                    option.OverrideEnabled = false;
                    System.AddonController.DisableOverride(option);
                }
                else {
                    option.OverrideEnabled = true;
                    System.AddonController.EnableOverride(option);
                }
            }
            System.Config.Save();
        }
                
        ImGui.SameLine();
        using (ImRaii.Disabled(option is null)) {
            using (Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetContentRegionAvail()) && option is not null) {
                    option.OverrideEnabled = false;
                    System.AddonController.DisableOverride(option);
                    System.Config.Overrides.Remove(option);
                    System.Config.Save();
                }
            }
        }
    }
    
    private static void DrawCopyConfigButton(OverrideConfig? option) {
        using var disabled = ImRaii.Disabled(option is null);
        
        if (ImGui.Button("Copy", new Vector2(ImGui.GetContentRegionMax().X * 1.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {
            var jsonString = JsonSerializer.Serialize(option, JsonSettings.SerializerOptions);
            var compressed = Util.CompressString(jsonString);
            ImGui.SetClipboardText(Convert.ToBase64String(compressed));
        }
            
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Copy Configuration");
        }
        ImGui.SameLine();
    }
    
    private void DrawPasteConfigButton(OverrideConfig? option) {
        if (ImGui.Button("Paste", new Vector2(ImGui.GetContentRegionMax().X * 1.0f / 6.0f, ImGui.GetContentRegionMax().Y))) {
            var decodedString = Convert.FromBase64String(ImGui.GetClipboardText());
            var uncompressed = Util.DecompressString(decodedString);

            if (uncompressed.IsNullOrEmpty()) {
                Services.NotificationManager.AddNotification(new Notification {
                    Type = NotificationType.Error, 
                    Content = "Unable to Paste Configuration",
                });
            }

            if (JsonSerializer.Deserialize<OverrideConfig>(uncompressed, JsonSettings.SerializerOptions) is { } pastedConfiguration) {
                
                // Overwrite the node path with the node we are trying to actually update.
                pastedConfiguration.NodePath = currentPath;
                
                // If we don't have an option already, make a new one with the new data
                if (System.Config.Overrides.All(existingOption => existingOption.NodePath != pastedConfiguration.NodePath)) {
                    System.Config.Overrides.Add(pastedConfiguration);
                    System.AddonController.EnableOverride(pastedConfiguration);
                }

                // If we do already have an option, set the values
                else {
                    pastedConfiguration.CopyTo(option);
                }

                Services.NotificationManager.AddNotification(new Notification {
                    Type = NotificationType.Info, 
                    Content = $"Pasted Configuration: {pastedConfiguration.NodePath}",
                });
            
                System.Config.Save();
            }
        }

        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Paste Configuration");
        }
    }
        
    private string? GetProxyNameForSelectedAddon() {
        var addonName = currentPath.Split("/").FirstOrDefault();
        if (addonName is null) return null;

        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
        if (addon is null) return null;

        if (addon->HostId is not 0) {
            var proxyAddon = RaptureAtkUnitManager.Instance()->GetAddonById(addon->HostId);
            if (proxyAddon is not null) {
                return proxyAddon->NameString;
            }
        }

        return null;
    }
}
