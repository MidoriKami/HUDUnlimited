using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace HUDUnlimited.Classes;

public unsafe class AddonListController : IDisposable {
    public List<Pointer<AtkUnitBase>> Addons { get; private set; } = [];

    public AddonListController() {
        Service.Framework.Update += OnFrameworkUpdate;
    }
    
    public void Dispose() {
        Service.Framework.Update -= OnFrameworkUpdate;
    }
    
    private void OnFrameworkUpdate(IFramework framework) {
        Addons = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries.ToArray().Where(entry => entry.Value is not null).ToList();
    }
}