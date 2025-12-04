using Dalamud.Game.Command;
using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;
using KamiToolKit;
using AddonController = HUDUnlimited.Classes.AddonController;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IDalamudPlugin {
    public HUDUnlimitedPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();
        
        System.Config = Configuration.Load();
        KamiToolKitLibrary.Initialize(pluginInterface);
        
        System.AddonController = new AddonController();
        
        System.ConfigWindow = new ConfigurationWindow {
            InternalName = "HUDUnlimitedConfig", 
            Title = "HUD Unlimited Configuration",
        };

        Service.CommandManager.AddHandler("/hudu", new CommandInfo(CommandHandler) {
            HelpMessage = "Open Configuration Window",
            ShowInHelp = true,
        });

        System.ConfigWindow.DebugOpen();
    }

    private static void CommandHandler(string command, string arguments) {
        switch (command) {
            case "/hudu":
                System.ConfigWindow.Toggle();
                break;
        }
    }

    public void Dispose() {
        Service.PluginInterface.UiBuilder.OpenConfigUi -= System.ConfigWindow.Toggle;

        Service.CommandManager.RemoveHandler("/hudu");

        System.AddonController.Dispose();
        
        KamiToolKitLibrary.Dispose();
    }
}
