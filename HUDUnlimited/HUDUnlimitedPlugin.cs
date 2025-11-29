using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;
using KamiLib.CommandManager;
using KamiLib.Window;
using KamiToolKit;
using AddonController = HUDUnlimited.Classes.AddonController;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IDalamudPlugin {
    public HUDUnlimitedPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();
        
        System.Config = Configuration.Load();
        KamiToolKitLibrary.Initialize(pluginInterface);

        System.OverrideListWindow = new OverrideListWindow();
        System.InfoWindow = new InfoWindow();
        
        System.AddonController = new AddonController();
        
        System.NativeConfigWindow = new NativeConfigWindow {
            InternalName = "HUDUnlimitedConfig", 
            Title = "HUD Unlimited Configuration",
        };
        
        System.WindowManager = new WindowManager(Service.PluginInterface);
        System.WindowManager.AddWindow(System.OverrideListWindow);
        System.WindowManager.AddWindow(System.InfoWindow);

        System.CommandManager = new CommandManager(Service.PluginInterface, "hudu", "hudunlimited");
        System.CommandManager.RegisterCommand(new CommandHandler {
            ActivationPath = "/",
            Delegate = _ => System.NativeConfigWindow.Toggle(),
        });

        Service.PluginInterface.UiBuilder.OpenConfigUi += System.NativeConfigWindow.Toggle;

        System.NativeConfigWindow.DebugOpen();
    }

    public void Dispose() {
        Service.PluginInterface.UiBuilder.OpenConfigUi -= System.NativeConfigWindow.Toggle;

        System.CommandManager.Dispose();
        System.AddonController.Dispose();
        System.WindowManager.Dispose();
        
        KamiToolKitLibrary.Dispose();
    }
}
