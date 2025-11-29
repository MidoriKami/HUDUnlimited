using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDUnlimited.Windows.Nodes;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace HUDUnlimited.Windows;

public unsafe class NativeConfigWindow : NativeAddon {

	private TabBarNode? tabBarNode;
	private AddonSelectTabNode? addonSelectTabNode;
    private PartSelectTabNode? partSelectTabNode;
	private VerticalLineNode? verticalLineNode;
    private PreviewNode? previewNode;

	protected override void OnSetup(AtkUnitBase* addon) {
		SetWindowSize(900.0f, 600.0f);
        
        var searchWidgetSize = ContentSize.X / 3.0f - 16.0f;

		tabBarNode = new TabBarNode {
			Position = ContentStartPosition, 
			Size = new Vector2(searchWidgetSize, 28.0f),
		};
		tabBarNode.AddTab("Window", OnWindowTabSelected);
		tabBarNode.AddTab("Part", OnPartTabSelected, false);
		tabBarNode.AddTab("Edit", OnEditTabSelected, false);
        tabBarNode.AttachNode(this);

        addonSelectTabNode = new AddonSelectTabNode {
            Position = new Vector2(tabBarNode.Bounds.Left, tabBarNode.Bounds.Bottom),
            Size = new Vector2(searchWidgetSize, ContentSize.Y - tabBarNode.Height - 4.0f),
            OnAddonConfirmed = OnAddonConfirmed,
            OnAddonSelected = addonName => OnAddonSelected(addonName, addon),
        };
        addonSelectTabNode.AttachNode(this);

        partSelectTabNode = new PartSelectTabNode {
            Position = new Vector2(tabBarNode.Bounds.Left, tabBarNode.Bounds.Bottom),
            Size = new Vector2(searchWidgetSize, ContentSize.Y - tabBarNode.Height - 4.0f),
            IsVisible = false,
        };
        partSelectTabNode.AttachNode(this);
		
		verticalLineNode = new VerticalLineNode {
			Position = ContentStartPosition + new Vector2(searchWidgetSize + 8.0f, 0.0f),
			Size = new Vector2(4.0f, ContentSize.Y),
		};
        verticalLineNode.AttachNode(this);

        previewNode = new PreviewNode {
            Position = new Vector2(verticalLineNode.Bounds.Right + 4.0f, verticalLineNode.Bounds.Top), 
            Size = new Vector2(ContentSize.X - searchWidgetSize - 16.0f, ContentSize.Y - 4.0f),
        };
        previewNode.AttachNode(this);
    }

    protected override void OnFinalize(AtkUnitBase* addon) {
        previewNode?.Reset();
    }

    private void OnAddonSelected(string addonName, AtkUnitBase* addon) {
        previewNode?.SetTargetAddon(addonName);
        addon->UldManager.UpdateDrawNodeList();
        addon->UpdateCollisionNodeList(false);
    }

    private void OnAddonConfirmed(string obj) {
        if (tabBarNode is null) return;

        OnPartTabSelected();

        tabBarNode.SelectTab("Part");

        tabBarNode.EnableTab("Window");
        tabBarNode.EnableTab("Part");
        tabBarNode.DisableTab("Edit");
    }

    private void OnWindowTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (partSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = true;
        partSelectTabNode.IsVisible = false;
    }

    private void OnPartTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (partSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = false;
        partSelectTabNode.IsVisible = true;
    }

    private void OnEditTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (partSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = false;
        partSelectTabNode.IsVisible = false;
    }
}
