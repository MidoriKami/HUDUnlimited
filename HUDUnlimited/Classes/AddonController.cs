using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDUnlimited.Classes;

public unsafe class AddonController : IDisposable {
    private readonly HashSet<string> trackedAddons = [];
    
    public AddonController() {
        foreach (var nodeOverride in System.Config.Overrides) {
            EnableOverride(nodeOverride);
        }
    }

    public void Dispose() {
        Service.AddonLifecycle.UnregisterListener(ApplyOverrides);
    }

    public void EnableOverride(OverrideConfig overrideConfig) {
        if (!trackedAddons.Any(addon => addon == overrideConfig.AttachAddonName)) {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, overrideConfig.AttachAddonName, ApplyOverrides);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, overrideConfig.AttachAddonName, ApplyOverrides);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, overrideConfig.AttachAddonName, ApplyOverrides);
            Service.PluginLog.Debug($"Registering Listener: {overrideConfig.AttachAddonName}");
            trackedAddons.Add(overrideConfig.AttachAddonName);
        }
        else {
            Service.PluginLog.Debug($"Listener already active for {overrideConfig.NodePath}:{overrideConfig.AttachAddonName}");
        }
    }

    public void DisableOverride(OverrideConfig overrideConfig) {
        var anyStillActive = System.Config.Overrides
            .Where(option => option.AddonName == overrideConfig.AttachAddonName)
            .Any(option => option.OverrideEnabled);

        if (!anyStillActive) {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, overrideConfig.AttachAddonName, ApplyOverrides);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, overrideConfig.AttachAddonName, ApplyOverrides);
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, overrideConfig.AttachAddonName, ApplyOverrides);
            trackedAddons.Remove(overrideConfig.AttachAddonName);
            Service.PluginLog.Debug($"Unregistering Listener: {overrideConfig.AttachAddonName}:{overrideConfig.AttachAddonName}");
        }
    }
    
    private void ApplyOverrides(AddonEvent type, AddonArgs args) {
        var options = System.Config.Overrides
            .Where(option => option.AttachAddonName == args.AddonName)
            .Where(option => option.OverrideEnabled);

        foreach (var option in options) {
            // If this option is for an Embedded Addon, and we are being called from the proxyParent. We need to fetch the correct addon.
            var targetAddon = (AtkUnitBase*)args.Addon;
            if (option.ProxyParentName is not null) {
                var proxyAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(option.AddonName);
                if (proxyAddon is not null) {
                    targetAddon = proxyAddon;
                }
            }
            
            // Get node to modify for this option
            var node = GetNode(ref targetAddon->UldManager, option.NodePath);
            if (node is null) {
                Service.PluginLog.Verbose($"Failed to find node: {option.NodePath}");
                continue;
            }
            
            // Apply overrides for this node
            if (option.Flags.HasFlag(OverrideFlags.Position)) {
                node->SetPositionFloat(option.Position.X, option.Position.Y);
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Scale)) {
                node->SetScale(option.Scale.X, option.Scale.Y);
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Color)) {
                node->Color = option.Color.ToByteColor();
            }

            if (option.Flags.HasFlag(OverrideFlags.AddColor) && option.Flags.HasFlag(OverrideFlags.SubtractColor)) {
                node->AddRed = (short)( option.AddColor.X * 255.0f - option.SubtractColor.X * 255.0f);
                node->AddGreen = (short)( option.AddColor.Y * 255.0f - option.SubtractColor.Y * 255.0f );
                node->AddBlue = (short)( option.AddColor.Z * 255.0f - option.SubtractColor.Z * 255.0f );
            }
            else {
                if (option.Flags.HasFlag(OverrideFlags.AddColor)) {
                    node->AddRed = (short)( option.AddColor.X * 255.0f );
                    node->AddGreen = (short)( option.AddColor.Y * 255.0f );
                    node->AddBlue = (short)( option.AddColor.Z * 255.0f );
                }
            
                if (option.Flags.HasFlag(OverrideFlags.SubtractColor)) {
                    node->AddRed = (short)( -option.SubtractColor.X * 255.0f );
                    node->AddGreen = (short)( -option.SubtractColor.Y * 255.0f );
                    node->AddBlue = (short)( -option.SubtractColor.Z * 255.0f );
                }
            }
            
            if (option.Flags.HasFlag(OverrideFlags.MultiplyColor)) {
                node->MultiplyRed = (byte)( option.MultiplyColor.X * 255.0f );
                node->MultiplyGreen = (byte)( option.MultiplyColor.Y * 255.0f );
                node->MultiplyBlue = (byte)( option.MultiplyColor.Z * 255.0f );
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Visibility)) {
                node->ToggleVisibility(option.Visible);
            }
        }
    }

    private static AtkResNode* GetNode(ref AtkUldManager manager, string path) {
        // Omit the addon name, we already matched it to this AtkUldManager
        var nodePath = path.Split("/")[1..];

        return GetNodeInner(ref manager, nodePath);
    }

    private static AtkResNode* GetNodeInner(ref AtkUldManager manager, string[] remainingPath) {
        switch (remainingPath.Length) {
            // We are at the last step in the path, get the node and return it
            case 1 when uint.TryParse(remainingPath[0], out var index):
                return FindNode(ref manager, index);

            // Else we need to keep stepping in
            case > 1 when uint.TryParse(remainingPath[0], out var index):
                var componentNode = (AtkComponentNode*) FindNode(ref manager, index);
                
                if (componentNode is null) {
                    Service.PluginLog.Warning("Encountered null node when one was expected");
                    return null;
                }

                return GetNodeInner(ref componentNode->Component->UldManager, remainingPath[1..]);
            
            default:
                Service.PluginLog.Warning("Unable to parse remaining path.");
                return null;
        }
    }

    private static AtkResNode* FindNode(ref AtkUldManager manager, uint nodeId) {
        for (var i = 0; i < manager.NodeListSize; i++) {
            var currentNode = manager.NodeList[i];
            
            if (currentNode is not null && currentNode->NodeId == nodeId) {
                return currentNode;
            }
        }
        return null;
    }
}