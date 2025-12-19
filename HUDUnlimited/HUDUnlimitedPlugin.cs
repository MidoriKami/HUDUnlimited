using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IDalamudPlugin {
    public HUDUnlimitedPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();
        
        System.Config = Configuration.Load();

        System.ConfigurationWindow = new ConfigurationWindow();
        System.OverrideListWindow = new OverrideListWindow();
        
        System.AddonController = new AddonController();
        
        System.WindowSystem = new WindowSystem("HUDUnlimited");
        System.WindowSystem.AddWindow(System.ConfigurationWindow);
        System.WindowSystem.AddWindow(System.OverrideListWindow);

        Services.CommandManager.AddHandler("/hudunlimited", new CommandInfo(OnCommand) {
            HelpMessage = "Open HUDUnlimited Config Window",
            ShowInHelp = true,
        });
        
        Services.CommandManager.AddHandler("/hudu", new CommandInfo(OnCommand) {
            HelpMessage = "Open HUDUnlimited Config Window",
            ShowInHelp = true,
        });
        
        Services.PluginInterface.UiBuilder.Draw += System.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
    }
    
    public void Dispose() {
        Services.PluginInterface.UiBuilder.OpenConfigUi -= System.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        
        System.WindowSystem.RemoveAllWindows();
        
        Services.CommandManager.RemoveHandler("/hudunlimited");
        Services.CommandManager.RemoveHandler("/hudu");
        
        System.AddonController.Dispose();
    }

    private static void OnCommand(string command, string arguments) {
        if (command is not ( "/hudunlimited" or "/hudu" )) return;
        
        System.ConfigurationWindow.IsOpen = !System.ConfigurationWindow.IsOpen;
    }
    
    private static void OpenConfigUi() {
        System.ConfigurationWindow.IsOpen = !System.ConfigurationWindow.IsOpen;
    }

    private static void OpenMainUi() {
        System.OverrideListWindow.IsOpen = !System.OverrideListWindow.IsOpen;
    }
}
