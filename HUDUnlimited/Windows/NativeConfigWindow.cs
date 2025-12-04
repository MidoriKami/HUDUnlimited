using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDUnlimited.Windows.Nodes;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace HUDUnlimited.Windows;

public unsafe class ConfigurationWindow : NativeAddon {

	private TabBarNode? tabBarNode;
	private AddonSelectTabNode? addonSelectTabNode;
    private ComponentSelectTabNode? componentSelectTabNode;
	private VerticalLineNode? verticalLineNode;
    private PreviewNode? previewNode;

	protected override void OnSetup(AtkUnitBase* addon) {
		SetWindowSize(1100.0f, 600.0f);
        
        var searchWidgetSize = ContentSize.X * 3.0f / 8.0f - 16.0f;

		tabBarNode = new TabBarNode {
			Position = ContentStartPosition, 
			Size = new Vector2(searchWidgetSize, 28.0f),
		};
		tabBarNode.AddTab("Window", OnWindowTabSelected);
        tabBarNode.AddTab("Component", OnComponentTabSelected, false);
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

        componentSelectTabNode = new ComponentSelectTabNode {
            Position = new Vector2(tabBarNode.Bounds.Left, tabBarNode.Bounds.Bottom),
            Size = new Vector2(searchWidgetSize, ContentSize.Y - tabBarNode.Height - 4.0f),
            IsVisible = false,
            OnComponentConfirmed = OnComponentConfirmed,
        };
        componentSelectTabNode.AttachNode(this);
		
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
        componentSelectTabNode?.Reset();
    }

    private void OnAddonSelected(string addonName, AtkUnitBase* addon) {
        if (tabBarNode is null) return;
        
        previewNode?.SetTargetAddon(addonName);
        addon->UldManager.UpdateDrawNodeList();
        addon->UpdateCollisionNodeList(false);
        
        componentSelectTabNode?.Reset();        
        
        tabBarNode.EnableTab("Window");
        tabBarNode.DisableTab("Component");
        tabBarNode.DisableTab("Part");
        tabBarNode.DisableTab("Edit");
    }

    // todo: decide if things should be cleared or relocked on re-select
    private void OnAddonConfirmed(string addonName) {
        if (tabBarNode is null) return;
        if (componentSelectTabNode is null) return;

        OnComponentTabSelected();
        componentSelectTabNode.BuildForAddon(addonName);

        tabBarNode.SelectTab("Component");

        tabBarNode.EnableTab("Window");
        tabBarNode.EnableTab("Component");
        tabBarNode.DisableTab("Part");
        tabBarNode.DisableTab("Edit");
    }
    
    private void OnComponentConfirmed(string obj) {
        if (tabBarNode is null) return;

        tabBarNode.SelectTab("Part");

        tabBarNode.EnableTab("Window");
        tabBarNode.EnableTab("Component");
        tabBarNode.EnableTab("Part");
        tabBarNode.DisableTab("Edit");
    }

    private void OnWindowTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (componentSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = true;
        componentSelectTabNode.IsVisible = false;
    }

    private void OnComponentTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (componentSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = false;
        componentSelectTabNode.IsVisible = true;
    }

    private void OnPartTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (componentSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = false;
        componentSelectTabNode.IsVisible = false;
    }

    private void OnEditTabSelected() {
        if (addonSelectTabNode is null) return;
        if (tabBarNode is null) return;
        if (componentSelectTabNode is null) return;

        addonSelectTabNode.IsVisible = false;
        componentSelectTabNode.IsVisible = false;
    }
}
