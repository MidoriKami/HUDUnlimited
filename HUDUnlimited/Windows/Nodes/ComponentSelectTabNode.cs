using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public unsafe class ComponentSelectTabNode : SimpleComponentNode {
    private ModifyListNode<ComponentInfoNode> componentListNode;
    private readonly HorizontalLineNode horizontalLineNode;
    private readonly TextButtonNode confirmButtonNode;
    private readonly TextButtonNode refreshButtonNode;

    public Action<string>? OnComponentConfirmed { get; set; }
    public Action<string>? OnComponentSelected { get; set; }

    private string loadedAddon = string.Empty;
    
    public ComponentSelectTabNode() {
        componentListNode = new ModifyListNode<ComponentInfoNode>();
        componentListNode.AttachNode(this);
        
        horizontalLineNode = new HorizontalLineNode();
        horizontalLineNode.AttachNode(this);

        refreshButtonNode = new TextButtonNode {
            String = "Refresh",
            OnClick = OnRefreshClicked,
        };
        refreshButtonNode.AttachNode(this);

        confirmButtonNode = new TextButtonNode {
            String = "Confirm",
            OnClick = OnConfirmClicked,
        };
        confirmButtonNode.AttachNode(this);
    }

    private void OnConfirmClicked() {
        
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        componentListNode.Size = Size - new Vector2(0.0f, 30.0f);
        componentListNode.Position = Vector2.Zero;

        horizontalLineNode.Size = new Vector2(Width, 4.0f);
        horizontalLineNode.Position = new Vector2(0.0f, Height - 38.0f);

        refreshButtonNode.Size = new Vector2(100.0f, 24.0f);
        refreshButtonNode.Position = new Vector2(0.0f, Height - 24.0f);

        confirmButtonNode.Size = new Vector2(100.0f, 24.0f);
        confirmButtonNode.Position = new Vector2(Width - 100.0f, Height - 24.0f);
    }

    private void OnRefreshClicked() {
        BuildForAddon(loadedAddon);
    }

    public void Reset() {
        componentListNode.SelectionOptions = [];
    }

    public void BuildForAddon(string addonName) {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
        if (addon is null) return;

        var rootNode = new ComponentInfoNode {
            Node = addon->RootNode,
            NodePath = $"{addonName}/root",
            Label = "Root Node",
        };

        componentListNode.SelectionOptions = BuildRecursively(&addon->UldManager, addon->NameString, [ rootNode ]);
        
        loadedAddon = addonName;
    }

    private List<ComponentInfoNode> BuildRecursively(AtkUldManager* uldManager, string progressivePath, List<ComponentInfoNode> componentList) {
        foreach (var node in uldManager->Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is not NodeType.Component) continue;

            var component = node.Value->GetComponent();
            if (component is null) continue;

            componentList.Add(new ComponentInfoNode {
                Node = node,
                NodePath = progressivePath + $"/{node.Value->NodeId}",
                Label = $"Component - {GetComponentType(component)}",
            });

            componentList.AddRange(BuildRecursively(&component->UldManager, progressivePath + $"/{node.Value->NodeId}", []));
        }
        
        return componentList;
    }

    private string GetComponentType(AtkComponentBase* component) {
        if (component is null) return string.Empty;

        var componentInfo = (AtkUldComponentInfo*)component->UldManager.Objects;
        return componentInfo->ComponentType.ToString();
    }
}
