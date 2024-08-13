using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility.Numerics;
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
        Service.AddonLifecycle.UnregisterListener(OnDrawOverride);
    }

    public void EnableOverride(NodeOverride nodeOverride) {
        if (!trackedAddons.Any(addon => addon == nodeOverride.AddonName)) {
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, nodeOverride.AddonName, OnDrawOverride);
            Service.PluginLog.Debug($"Registering Listener: {nodeOverride.AddonName}");
            trackedAddons.Add(nodeOverride.AddonName);
        }
        else {
            Service.PluginLog.Debug($"Listener already active for {nodeOverride.NodePath}");
        }
    }

    public void DisableOverride(NodeOverride nodeOverride) {
        var anyStillActive = System.Config.Overrides
            .Where(option => option.AddonName == nodeOverride.AddonName)
            .Any(option => option.OverrideEnabled);

        if (!anyStillActive) {
            Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, nodeOverride.AddonName, OnDrawOverride);
            trackedAddons.Remove(nodeOverride.AddonName);
            Service.PluginLog.Debug($"Unregistering Listener: {nodeOverride.AddonName}");
        }
    }
    
    private void OnDrawOverride(AddonEvent type, AddonArgs args) {
        var options = System.Config.Overrides
            .Where(option => option.AddonName == args.AddonName)
            .Where(option => option.OverrideEnabled);

        foreach (var option in options) {
            // Get node to modify for this option
            var node = GetNode(ref ((AtkUnitBase*) args.Addon)->UldManager, option.NodePath);
            if (node is null) {
                Service.PluginLog.Warning($"Failed to find node: {option.NodePath}");
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
            
            if (option.Flags.HasFlag(OverrideFlags.AddColor)) {
                node->AddRed = (short)( option.AddColor.X * 255.0f );
                node->AddGreen = (short)( option.AddColor.Y * 255.0f );
                node->AddBlue = (short)( option.AddColor.Z * 255.0f );
            }
            
            if (option.Flags.HasFlag(OverrideFlags.MultiplyColor)) {
                node->MultiplyRed = (byte)( option.MultiplyColor.X * 100.0f );
                node->MultiplyGreen = (byte)( option.MultiplyColor.Y * 100.0f );
                node->MultiplyBlue = (byte)( option.MultiplyColor.Z * 100.0f );
            }
            
            if (option.Flags.HasFlag(OverrideFlags.Visibility)) {
                node->ToggleVisibility(option.Visible);
            }
        }
    }

    private AtkResNode* GetNode(ref AtkUldManager manager, string path) {
        // Omit the addon name, we already matched it to this AtkUldManager
        var nodePath = path.Split("/")[1..];

        return GetNodeInner(ref manager, nodePath);
    }

    private AtkResNode* GetNodeInner(ref AtkUldManager manager, string[] remainingPath) {
        switch (remainingPath.Length) {
            // We are at the last step in the path, get the node and return it
            case 1 when uint.TryParse(remainingPath[0], out var index):
                return manager.SearchNodeById(index);

            // Else we need to keep stepping in
            case > 1 when uint.TryParse(remainingPath[0], out var index): 
                var componentNode = (AtkComponentNode*) manager.SearchNodeById(index);
                
                if (componentNode is null) {
                    Service.PluginLog.Warning("Encountered null node when one was expected");
                    return null;
                }

                return GetNodeInner(ref manager, remainingPath[1..]);
            
            default:
                Service.PluginLog.Warning("Unable to parse remaining path.");
                return null;
        }
    }
}