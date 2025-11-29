using System;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public unsafe class PreviewNode : SimpleComponentNode {

    private AlphaImageNode backgroundImageNode;
    private SimpleOverlayNode previewContainer;
    private SliderNode colorSliderNode;
    private SliderNode scaleSliderNode;
    
    private AtkResNode* capturedNode;

    public PreviewNode() {
        backgroundImageNode = new AlphaImageNode();
        backgroundImageNode.AttachNode(this);

        previewContainer = new SimpleOverlayNode();
        previewContainer.AttachNode(this);

        colorSliderNode = new SliderNode {
            Range = ..255,
            OnValueChanged = newValue => {
                backgroundImageNode.AddColor = -new Vector3(newValue, newValue, newValue) / 255.0f;
                
                if (newValue is 0) {
                    backgroundImageNode.MultiplyColor = new Vector3(2.0f, 2.0f, 2.0f);
                }
                else {
                    backgroundImageNode.MultiplyColor = Vector3.One;
                }
            },
            Value = 100,
        };
        colorSliderNode.AttachNode(this);

        scaleSliderNode = new SliderNode {
            Range = 1000..50000,
            DecimalPlaces = 2,
            OnValueChanged = _ => AdjustPreviewNodePosition(capturedNode),
            Value = 10000,
            Step = 500,
        };
        scaleSliderNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        previewContainer.Size = Size - new Vector2(20.0f, 20.0f) - new Vector2(0.0f, 30.0f);
        previewContainer.Position = new Vector2(10.0f, 10.0f);
        
        backgroundImageNode.Size = previewContainer.Size;
        backgroundImageNode.Position = previewContainer.Position;

        colorSliderNode.Size = new Vector2(200.0f, 24.0f);
        colorSliderNode.Position = new Vector2(10.0f, Height - colorSliderNode.Height - 5.0f);

        scaleSliderNode.Size = new Vector2(200.0f, 24.0f);
        scaleSliderNode.Position = new Vector2(Width - scaleSliderNode.Width - 35.0f, Height - scaleSliderNode.Height - 5.0f);
    }

    public void SetTargetAddon(string addonName) {
        if (capturedNode is not null) {
            Reset();
        }

        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
        if (addon is null) return;

        AttachNode(addon->RootNode, addonName);
    }

    public void Reset() {
        ResetStolenNode();
        Service.AddonLifecycle.UnregisterListener(OnAddonFinalize);
    }

    private void AttachNode(AtkResNode* node, string attachedAddon) {
        if (node is null) return;

        scaleSliderNode.Value = 10000;
        
        previewContainer.CollisionNode.Node->PrevSiblingNode = node;
        previewContainer.MarkDirty();
        previewContainer.ComponentBase->UldManager.UpdateDrawNodeList();

        AdjustPreviewNodePosition(node);

        capturedNode = node;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, attachedAddon, OnAddonFinalize);
    }

    private void AdjustPreviewNodePosition(AtkResNode* node) {
        if (node is null) return;
        
        var nodeSize = new Vector2(node->Width, node->Height);
        var availableSize = Size - new Vector2(20.0f, 20.0f);
        var scaleAdjustment = availableSize / nodeSize;

        if (nodeSize.X < availableSize.X) {
            scaleAdjustment.X = 1.0f;
        }

        if (nodeSize.Y < availableSize.Y) {
            scaleAdjustment.Y = 1.0f;
        }

        var scaleMultiplier = scaleSliderNode.Value / 10000.0f;

        var minFactor = MathF.Min(scaleAdjustment.X, scaleAdjustment.Y);
        var scaleOffset = new Vector2(minFactor, minFactor) * scaleMultiplier;
        
        var hijackedNodePosition = new Vector2(node->X, node->Y) * scaleOffset;
        var centerOffset = availableSize / 2.0f - nodeSize / 2.0f * scaleOffset;

        previewContainer.Position = -hijackedNodePosition + new Vector2(10.0f, 10.0f) + centerOffset;
        previewContainer.Scale = scaleOffset;
    }

    private void ResetStolenNode() {
        if (capturedNode is null) return;
        
        previewContainer.CollisionNode.Node->PrevSiblingNode = null;

        capturedNode->SetPositionFloat(capturedNode->X + 1, capturedNode->Y + 1);
        capturedNode->SetPositionFloat(capturedNode->X, capturedNode->Y);
        capturedNode = null;

        Service.AddonLifecycle.UnregisterListener(OnAddonFinalize);
    }
    
    private void OnAddonFinalize(AddonEvent type, AddonArgs args)
        => Reset();
}
