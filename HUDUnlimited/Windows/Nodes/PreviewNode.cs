using System;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public unsafe class PreviewNode : SimpleComponentNode {

    private AlphaImageNode backgroundImageNode;
    private SimpleOverlayNode previewContainer;
    private SliderNode colorSliderNode;
    
    private AtkResNode* capturedNode;
    private ViewportEventListener eventListener;

    private float zoomFactor = 1.0f;
    private bool dragStarted;
    private Vector2 clickStart;
    private Vector2 cumulativeOffset;
    
    public PreviewNode() {
        eventListener = new ViewportEventListener(OnViewportEvent);
        
        backgroundImageNode = new AlphaImageNode();
        backgroundImageNode.AttachNode(this);
        
        backgroundImageNode.AddEvent(AtkEventType.MouseOver, () => Service.AddonEventManager.SetCursor(AddonCursorType.Hand));
        backgroundImageNode.AddEvent(AtkEventType.MouseOut, () => Service.AddonEventManager.ResetCursor());
        backgroundImageNode.AddEvent(AtkEventType.MouseWheel, (_, _, _, _, data) => {
            zoomFactor += data->MouseData.WheelDirection * 0.10f;
            AdjustPreviewNodePosition(capturedNode, cumulativeOffset);
        });

        backgroundImageNode.AddEvent(AtkEventType.MouseDown, (_, _, _, _, data)  => {
            if (!dragStarted) {
                eventListener.AddEvent(AtkEventType.MouseMove, backgroundImageNode);
                eventListener.AddEvent(AtkEventType.MouseUp, backgroundImageNode);

                Service.AddonEventManager.SetCursor(AddonCursorType.Grab);
                clickStart = new Vector2(data->MouseData.PosX, data->MouseData.PosY);
                dragStarted = true;
            }
        });

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
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        previewContainer.Size = Size - new Vector2(20.0f, 20.0f) - new Vector2(0.0f, 30.0f);
        previewContainer.Position = new Vector2(10.0f, 10.0f);
        
        backgroundImageNode.Size = previewContainer.Size;
        backgroundImageNode.Position = previewContainer.Position;

        colorSliderNode.Size = new Vector2(200.0f, 24.0f);
        colorSliderNode.Position = new Vector2(10.0f, Height - colorSliderNode.Height - 5.0f);
    }

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        base.Dispose(disposing, isNativeDestructor);
        eventListener.Dispose();
    }

    private void OnViewportEvent(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        switch (eventType) {
            case AtkEventType.MouseMove when dragStarted:
                var newPosition = new Vector2(atkEventData->MouseData.PosX, atkEventData->MouseData.PosY);
                var delta = newPosition - clickStart;
                cumulativeOffset += delta;
                clickStart = newPosition;
                previewContainer.Position += delta;
                break;
            
            case AtkEventType.MouseUp:
                eventListener.RemoveEvent(AtkEventType.MouseMove);
                eventListener.RemoveEvent(AtkEventType.MouseUp);

                if (backgroundImageNode.CheckCollision(atkEventData)) {
                    Service.AddonEventManager.SetCursor(AddonCursorType.Hand);
                }
                else {
                    Service.AddonEventManager.ResetCursor();
                }
                
                dragStarted = false;
                break;
        }
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
        eventListener.RemoveEvent(AtkEventType.MouseMove);
        eventListener.RemoveEvent(AtkEventType.MouseUp);
    }

    private void AttachNode(AtkResNode* node, string attachedAddon) {
        if (node is null) return;

        zoomFactor = 1.0f;
        
        previewContainer.CollisionNode.Node->PrevSiblingNode = node;
        previewContainer.MarkDirty();
        previewContainer.ComponentBase->UldManager.UpdateDrawNodeList();

        cumulativeOffset = Vector2.Zero;
        AdjustPreviewNodePosition(node);

        capturedNode = node;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, attachedAddon, OnAddonFinalize);
    }

    private void AdjustPreviewNodePosition(AtkResNode* node, Vector2? offset = null) {
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

        var minFactor = MathF.Min(scaleAdjustment.X, scaleAdjustment.Y);
        var scaleOffset = new Vector2(minFactor, minFactor) * zoomFactor;
        
        var hijackedNodePosition = new Vector2(node->X, node->Y) * scaleOffset;
        var centerOffset = availableSize / 2.0f - nodeSize / 2.0f * scaleOffset;
        
        offset ??= Vector2.Zero;

        previewContainer.Position = -hijackedNodePosition + new Vector2(10.0f, 10.0f) + centerOffset + offset.Value;
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
