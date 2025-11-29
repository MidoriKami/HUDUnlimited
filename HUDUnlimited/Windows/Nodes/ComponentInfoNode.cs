using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Premade.Nodes;

namespace HUDUnlimited.Windows.Nodes;

public unsafe class ComponentInfoNode : StringInfoNode {

    public required AtkResNode* Node { get; init; }
    public required string NodePath { get; init; }

    public override string GetSubLabel()
        => NodePath;

    public override uint? GetId()
        => null;

    public override uint? GetIconId()
        => Node is null ? null : Node->IsVisible() ? (uint) 60071 : 60072;

    public override string? GetTexturePath()
        => null;
}
