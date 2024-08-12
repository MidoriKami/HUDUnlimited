using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;
using KamiLib.Window;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IDalamudPlugin {
    public HUDUnlimitedPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();
        
        System.Config = Configuration.Load();

        System.ConfigurationWindow = new ConfigurationWindow();
        
        System.AddonListController = new AddonListController();

        System.WindowManager = new WindowManager(Service.PluginInterface);
        System.WindowManager.AddWindow(System.ConfigurationWindow, WindowFlags.OpenImmediately);
    }

    public void Dispose() {
        System.WindowManager.Dispose();
        System.AddonListController.Dispose();
    }
}