using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDUnlimited.Classes;

public static unsafe class AtkUldManagerExtensions {
    public static AtkResNode* GetNode(ref this AtkUldManager manager, string path) {
        if (manager.LoadedState is not AtkLoadState.Loaded) return null;
        
        // Omit the addon name, we already matched it to this AtkUldManager
        var nodePath = path.Split("/")[1..];

        return GetNodeInner(ref manager, nodePath);
    }

    private static AtkResNode* GetNodeInner(ref this AtkUldManager manager, string[] remainingPath) {
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
                
                if (componentNode->Type < (NodeType) 1000) {
                    Service.PluginLog.Warning("Encountered regular node when component node was expected");
                    return null;
                }

                return GetNodeInner(ref componentNode->Component->UldManager, remainingPath[1..]);
            
            default:
                Service.PluginLog.Warning("Unable to parse remaining path.");
                return null;
        }
    }

    private static AtkResNode* FindNode(this ref AtkUldManager manager, uint nodeId) {
        for (var i = 0; i < manager.NodeListSize; i++) {
            var currentNode = manager.NodeList[i];
            
            if (currentNode is not null && currentNode->NodeId == nodeId) {
                return currentNode;
            }
        }
        return null;
    }
}
