using System.Numerics;
using KamiToolKit.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public class PartSelectTabNode : SimpleComponentNode {
    private ScrollingAreaNode<TabbedVerticalListNode> partsListNode;

    public PartSelectTabNode() {
        partsListNode = new ScrollingAreaNode<TabbedVerticalListNode> {
            ContentHeight = 100.0f,
        };
        partsListNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        partsListNode.Size = Size - new Vector2(0.0f, 30.0f);
        partsListNode.Position = Vector2.Zero;
    }
}
