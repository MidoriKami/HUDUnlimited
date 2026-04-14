using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDUnlimited.Classes;

public static unsafe class NodeFinder {
    public static AtkResNode* GetNode(string fullPath) {
        var addonName = fullPath.Split("/").FirstOrDefault();
        if (addonName is null) return null;
        
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
        if (addon is null) return null;
        
        return GetNode(&addon->UldManager, fullPath);
    }
    
    public static AtkResNode* GetNode(AtkUldManager* manager, string path) {
        if (manager->LoadedState is not AtkLoadState.Loaded) return null;
        
        // Omit the addon name, we already matched it to this AtkUldManager
        var nodePath = path.Split("/")[1..];

        return GetNodeInner(manager, nodePath, path);
    }

    private static AtkResNode* GetNodeInner(AtkUldManager* manager, string[] remainingPath, string originalPath) {
        switch (remainingPath.Length) {
            // We are at the last step in the path, get the node and return it
            case 1 when uint.TryParse(remainingPath[0], out var index):
                return FindNode(manager, index);

            // Else we need to keep stepping in
            case > 1 when uint.TryParse(remainingPath[0], out var index):
                var componentNode = (AtkComponentNode*) FindNode(manager, index);

                if (componentNode is null) {
                    Services.PluginLog.Warning($"Encountered null node when one was expected. Path: {originalPath}");
                    return null;
                }
                
                if (componentNode->GetNodeType() is not NodeType.Component) {
                    Services.PluginLog.Warning($"Encountered regular node when component node was expected. Path: {originalPath}");
                    return null;
                }

                return GetNodeInner(&componentNode->Component->UldManager, remainingPath[1..], originalPath);
            
            default:
                return null;
        }
    }

    private static AtkResNode* FindNode(AtkUldManager* manager, uint nodeId) {
        for (var i = 0; i < manager->NodeListSize; i++) {
            var currentNode = manager->NodeList[i];
            
            if (currentNode is not null && currentNode->NodeId == nodeId) {
                return currentNode;
            }
        }
        return null;
    }
}
