using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Extensions;

namespace HUDUnlimited.Classes;

public unsafe class AddonController : IDisposable {
    private readonly HashSet<string> trackedAddons = [];
    
    public AddonController() {
        foreach (var nodeOverride in System.Config.Overrides) {
            EnableOverride(nodeOverride);
        }
    }

    public void Dispose() {
        Services.AddonLifecycle.UnregisterListener(ApplyOverrides);
    }

    public void EnableOverride(OverrideConfig overrideConfig) {
        if (!trackedAddons.Any(addon => addon == overrideConfig.AttachAddonName)) {
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, overrideConfig.AttachAddonName, ApplyOverrides);
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, overrideConfig.AttachAddonName, ApplyOverrides);
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, overrideConfig.AttachAddonName, ApplyOverrides);
            Services.PluginLog.Debug($"Registering Listener: {overrideConfig.AttachAddonName}");
            trackedAddons.Add(overrideConfig.AttachAddonName);
        }
        else {
            Services.PluginLog.Debug($"Listener already active for {overrideConfig.NodePath}:{overrideConfig.AttachAddonName}");
        }
    }

    public void DisableOverride(OverrideConfig overrideConfig) {
        var anyStillActive = System.Config.Overrides
            .Where(option => option.AddonName == overrideConfig.AttachAddonName)
            .Any(option => option.OverrideEnabled);

        if (!anyStillActive) {
            Services.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, overrideConfig.AttachAddonName, ApplyOverrides);
            Services.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, overrideConfig.AttachAddonName, ApplyOverrides);
            Services.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, overrideConfig.AttachAddonName, ApplyOverrides);
            trackedAddons.Remove(overrideConfig.AttachAddonName);
            Services.PluginLog.Debug($"Unregistering Listener: {overrideConfig.NodePath}:{overrideConfig.AttachAddonName}");
        }
    }
    
    private void ApplyOverrides(AddonEvent type, AddonArgs args) {
        var options = System.Config.Overrides
            .Where(option => option.AttachAddonName == args.AddonName)
            .Where(option => option.OverrideEnabled);

        foreach (var option in options) {
            // If this option is for an Embedded Addon, and we are being called from the proxyParent. We need to fetch the correct addon.
            var targetAddon = (AtkUnitBase*)args.Addon.Address;
            
            // Check that the addon we want to modify is completely ready yet.
            if (!targetAddon->IsReady) continue;
            
            if (option.ProxyParentName is not null) {
                var proxyAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(option.AddonName);
                if (proxyAddon is not null) {
                    targetAddon = proxyAddon;
                }
            }
            
            // Get node to modify for this option
            var node = NodeFinder.GetNode(&targetAddon->UldManager, option.NodePath);
            if (node is null) continue;
            
            // Apply overrides for this node
            if (option.Flags.HasFlag(OverrideFlags.Position)) {
                node->Position = option.Position;
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Scale)) {
                node->Scale = option.Scale;
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Color)) {
                node->ColorVector = option.Color;
            }

            if (option.Flags.HasFlag(OverrideFlags.AddColor) || option.Flags.HasFlag(OverrideFlags.SubtractColor)) {
                var addAmount = option.Flags.HasFlag(OverrideFlags.AddColor) ? option.AddColor : Vector3.Zero;
                var subtractAmount = option.Flags.HasFlag(OverrideFlags.SubtractColor) ? option.SubtractColor : Vector3.Zero;
                node->AddColor = addAmount - subtractAmount;
            }

            if (option.Flags.HasFlag(OverrideFlags.MultiplyColor)) {
                node->MultiplyColor = option.MultiplyColor;
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Visibility)) {
                node->ToggleVisibility(option.Visible);
            }

            if ((option.IsTextNode ?? false) && node->GetNodeType() is NodeType.Text) {
                var textNode = (AtkTextNode*) node;
                
                if (option.Flags.HasFlag(OverrideFlags.TextColor)) {
                    textNode->TextColor = option.TextColor.ToByteColor();
                }

                if (option.Flags.HasFlag(OverrideFlags.TextOutlineColor)) {
                    textNode->EdgeColor = option.TextOutlineColor.ToByteColor();
                }

                if (option.Flags.HasFlag(OverrideFlags.TextBackgroundColor)) {
                    textNode->BackgroundColor = option.TextBackgroundColor.ToByteColor();
                }

                if (option.Flags.HasFlag(OverrideFlags.FontSize)) {
                    textNode->FontSize = (byte) option.FontSize;
                }

                if (option.Flags.HasFlag(OverrideFlags.FontType)) {
                    textNode->SetFont(option.FontType);
                }

                if (option.Flags.HasFlag(OverrideFlags.AlignmentType)) {
                    textNode->SetAlignment(option.AlignmentType);
                }

                if (option.Flags.HasFlag(OverrideFlags.TextFlags)) {
                    textNode->TextFlags = option.TextFlags;
                }
            }
        }
    }
}
