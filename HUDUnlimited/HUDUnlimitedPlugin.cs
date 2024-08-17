using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;
using KamiLib.CommandManager;
using KamiLib.Window;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IDalamudPlugin {
    public HUDUnlimitedPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();
        
        System.Config = Configuration.Load();

        System.ConfigurationWindow = new ConfigurationWindow();
        System.OverrideListWindow = new OverrideListWindow();
        
        System.AddonController = new AddonController();
        
        System.WindowManager = new WindowManager(Service.PluginInterface);
        System.WindowManager.AddWindow(System.ConfigurationWindow, WindowFlags.OpenImmediately);
        System.WindowManager.AddWindow(System.OverrideListWindow, WindowFlags.OpenImmediately);

        System.CommandManager = new CommandManager(Service.PluginInterface, "hudu", "hudunlimited");
        System.CommandManager.RegisterCommand(new CommandHandler {
            ActivationPath = "/",
            Delegate = _ => System.ConfigurationWindow.UnCollapseOrToggle(),
        });

        Service.PluginInterface.UiBuilder.OpenConfigUi += System.ConfigurationWindow.UnCollapseOrToggle;
    }

    public void Dispose() {
        Service.PluginInterface.UiBuilder.OpenConfigUi -= System.ConfigurationWindow.UnCollapseOrToggle;

        System.CommandManager.Dispose();
        System.AddonController.Dispose();
        System.WindowManager.Dispose();
    }
}