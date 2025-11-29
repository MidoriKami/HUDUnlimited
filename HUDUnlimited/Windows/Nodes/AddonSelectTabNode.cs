using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public unsafe class AddonSelectTabNode : SimpleComponentNode {

    private readonly ModifyListNode<AddonStringInfoNode> listNode;
    private readonly HorizontalLineNode horizontalLineNode;
    private readonly TextButtonNode confirmButtonNode;
    private readonly TextButtonNode refreshButtonNode;
    
    public Action<string>? OnAddonConfirmed { get; set; }
    public Action<string>? OnAddonSelected { get; set; }
    public Action? OnRefresh { get; set; }

    public AddonSelectTabNode() {
        listNode = new ModifyListNode<AddonStringInfoNode> {
            SelectionOptions = GetOptions(),
            SortOptions = [ "Visibility", "Alphabetical" ],
            OnOptionChanged = newOption => {
                if (newOption is not null) {
                    OnAddonSelected?.Invoke(newOption.Label);
                }
            },
        };
        listNode.AttachNode(this);

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

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        listNode.Size = Size - new Vector2(0.0f, 32.0f);
        listNode.Position = new Vector2(0.0f, 0.0f);
        listNode.UpdateList();

        horizontalLineNode.Size = new Vector2(Width, 4.0f);
        horizontalLineNode.Position = new Vector2(0.0f, Height - 38.0f);

        refreshButtonNode.Size = new Vector2(100.0f, 24.0f);
        refreshButtonNode.Position = new Vector2(0.0f, Height - 24.0f);

        confirmButtonNode.Size = new Vector2(100.0f, 24.0f);
        confirmButtonNode.Position = new Vector2(Width - 100.0f, Height - 24.0f);
    }

    private void OnRefreshClicked() {
        listNode.SelectionOptions = GetOptions();
        OnRefresh?.Invoke();
    }

    private void OnConfirmClicked() {
        if (listNode.SelectedOption is not {} selectedOption) return;

        OnAddonConfirmed?.Invoke(selectedOption.Label);
    }
    
    private static List<AddonStringInfoNode> GetOptions() {
        List<AddonStringInfoNode> results = [];

        foreach (var unit in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (unit.Value is null) continue;
            if (!unit.Value->IsReady) continue;
            if (unit.Value->NameString == "HUDUnlimitedConfig") continue; // Very bad.
            if (unit.Value->NameString.Contains("KTK_Overlay")) continue; // Also bad.
            
            results.Add(new AddonStringInfoNode {
                Label = unit.Value->NameString,
            });
        }

        return results;
    }
}
