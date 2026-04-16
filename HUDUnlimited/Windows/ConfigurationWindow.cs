using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDUnlimited.Classes;
using HUDUnlimited.WindowParts;
using HUDUnlimited.Extensions;

namespace HUDUnlimited.Windows;

public unsafe class ConfigurationWindow : Window {

    private string currentPath = string.Empty;
    private AtkUldManager* currentNodeManager;
    private AtkResNode* currentNode;

    private readonly AddonSelect addonSelect;
    private readonly ComponentSelect componentSelect;
    private readonly NodeConfiguration nodeConfiguration;
    
    public ConfigurationWindow() : base("HUDUnlimited Configuration Window") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(600.0f, 400.0f),
        };
        
        TitleBarButtons.Add(new TitleBarButton {
            Click = _ => System.OverrideListWindow.IsOpen = !System.OverrideListWindow.IsOpen,
            Icon = FontAwesomeIcon.Cog,
            ShowTooltip = () => ImGui.SetTooltip("Open Preset Browser"),
            IconOffset = new Vector2(2.0f, 1.0f),
        });

        addonSelect = new AddonSelect {
            OnAddonSelected = OnAddonSelected,
        };

        componentSelect = new ComponentSelect {
            OnNodeSelected = OnNodeSelected,
        };

        nodeConfiguration = new NodeConfiguration();
    }

    private void OnNodeSelected(Pointer<AtkResNode> node) {
        if (node.Value is null) return;

        if (node.Value->GetNodeType() is NodeType.Component) {
            currentNodeManager = &node.Value->GetAsAtkComponentNode()->Component->UldManager;
            currentPath += $"/{node.Value->NodeId}";
        }
        else {

            // If we already had a normal node selected, replace the last part of the path.
            if (currentNode is not null && currentNode->GetNodeType() is not NodeType.Component) {
                currentPath = string.Join("/", currentPath.Split('/')[..^1]);
            }

            currentPath += $"/{node.Value->NodeId}";
        }

        currentNode = node.Value;
    }

    public override void Draw() {
        var headerHeight = 25.0f * ImGuiHelpers.GlobalScale;

        using (var headerChild = ImRaii.Child("HeaderChild", new Vector2(ImGui.GetContentRegionAvail().X, headerHeight))) {
            if (headerChild.Success) {
                DrawHeader();
            }
        }
        
        ImGui.Separator();

        using (var bodyChild = ImRaii.Child("BodyChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
            if (bodyChild.Success) {
                using var table = ImRaii.Table("LayoutTable", 2, ImGuiTableFlags.Resizable, ImGui.GetContentRegionAvail());
                if (table.Success) {
                    ImGui.TableSetupColumn("Selection", ImGuiTableColumnFlags.WidthStretch, 3.0f);
                    ImGui.TableSetupColumn("Settings", ImGuiTableColumnFlags.WidthStretch, 4.0f);

                    ImGui.TableNextColumn();
                    DrawSelection();

                    ImGui.TableNextColumn();
                    if (currentNode is not null) {
                        DrawConfig();
                    }
                }
            }
        }
    }

    private void DrawHeader() {
        if (currentPath == string.Empty) {
            ImGui.AlignTextToFramePadding();
            ImGuiHelpers.CenteredText("Select a window below to proceed. The window must be loaded to edit it.");
        }
        else {
            if (ImGui.Button("Clear Path", ImGuiHelpers.ScaledVector2(100.0f, ImGui.GetFrameHeight()))) {
                currentPath = string.Empty;
                currentNodeManager = null;
                return;
            }

            ImGui.SameLine(125.0f * ImGuiHelpers.GlobalScale);
            
            DrawPath(currentPath);
        }
    }

    private void DrawSelection() {
        if (currentNodeManager is null) {
            addonSelect.Draw();
        }
        else {
            componentSelect.Draw(currentNodeManager, currentNode, currentPath);

            if (currentNode is not null && currentNode->GetNodeType() is not NodeType.Component) {
                currentNode->DrawBorder(KnownColor.White.Vector(), 2.0f);
            }
        }

        ImGuiHelpers.ScaledDummy(2.0f);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.DragFloat("##LineThickness", ref System.Config.LineThickness, 0.01f, 0.5f, 10.0f);
    }

    private void DrawConfig() {

        using var configChild = ImRaii.Child("Config", ImGui.GetContentRegionAvail());
        if (!configChild.Success) return;
        
        nodeConfiguration.Draw(currentNode, currentPath);
    }

    
    private void OnAddonSelected(Pointer<AtkUnitBase> pointer) {
        if (pointer.Value is null) return;
        
        currentPath = pointer.Value->NameString;
        currentNodeManager = &pointer.Value->UldManager;
        currentNode = null;
    }

    private void DrawPath(string path) {
        var segments = path.Split('/');
        var pathSoFar = string.Empty;
        
        foreach (var (index, pathSegment) in segments.Index()) {
            using var id = ImRaii.PushId(pathSoFar);
            
            if (pathSoFar != string.Empty) {
                pathSoFar += "/";
                
                ImGui.SameLine();
            }

            pathSoFar += pathSegment;

            using (ImRaii.Disabled(index > segments.Length - 2)) {
                if (ImGui.Button(pathSegment)) {
                    ResetToPath(pathSoFar);
                }
            }

            if (index < segments.Length - 1) {
                ImGui.SameLine();
                
                ImGui.AlignTextToFramePadding();
                ImGui.Text("/");
            }
        }
    }

    /// <summary>
    /// Reset the current path, AtkUldManager, and selected node according to the passed in path.
    /// </summary>
    /// <param name="pathSoFar">Path to reset to.</param>
    private void ResetToPath(string pathSoFar) {
        // If we selected an addon, not a node
        if (pathSoFar.Split('/') is [ var addonName ]) {
            currentNode = null;
            
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
            if (addon is null) {
                currentPath = string.Empty;
                currentNodeManager = null;
                return;
            }

            currentNodeManager = &addon->UldManager;
            currentPath = pathSoFar;
            return;
        }
        
        // Get Node
        var node = NodeFinder.GetNode(pathSoFar);
        
        // If node is null, we errored. Reset.
        if (node is null) {
            currentNode = null;
            currentNodeManager = null;
            currentPath = string.Empty;
            return;
        }
        
        // If node is Component, load atkuldmanager
        if (node->GetNodeType() is NodeType.Component) {
            var componentNode = node->GetAsAtkComponentNode();
            if (componentNode is null) return; // Shouldn't happen.
            
            currentNodeManager = &componentNode->Component->UldManager;
            currentNode = node;
            currentPath = pathSoFar;
        }
        
        // If node is normal node, find its parent
        else if (node->GetNodeType() is not NodeType.Component) {
            
            // Loop to find Parent ComponentNode
            var parentNode = node->ParentNode;

            while (parentNode is not null) {
                if (parentNode->GetNodeType() is NodeType.Component) {
                    break; // We found it.
                }
                
                parentNode = parentNode->ParentNode;
            }

            // We don't have a component node parent, must be an addon instead.
            if (parentNode is null) {
                var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
                if (addon is null) return; // Something is broken if we are here.

                currentNodeManager = &addon->UldManager;
                currentNode = node;
                currentPath = pathSoFar;
            }
            
            // We found our parent component node
            else {
                var componentNode = parentNode->GetAsAtkComponentNode();
                if (componentNode is null) return; // Or not? Wtf?
                
                currentNodeManager = &componentNode->Component->UldManager;
                currentNode = node;
                currentPath = pathSoFar;
            }
        }
    }
}
