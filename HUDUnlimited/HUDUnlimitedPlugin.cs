using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using HUDUnlimited.Classes;
using HUDUnlimited.Extensions;
using HUDUnlimited.Windows;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IAsyncDalamudPlugin {
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    public Task LoadAsync(CancellationToken cancellationToken) {
        PluginInterface.Create<Services>();

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

        if (System.Config.DebugMode) {
            System.ConfigurationWindow.IsOpen = true;
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() {
        Services.PluginInterface.UiBuilder.OpenConfigUi -= System.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;

        System.WindowSystem.RemoveAllWindows();

        Services.CommandManager.RemoveHandler("/hudunlimited");
        Services.CommandManager.RemoveHandler("/hudu");

        System.AddonController.Dispose();

        return ValueTask.CompletedTask;
    }

    private static void OnCommand(string command, string arguments) {
        if (command is not ("/hudunlimited" or "/hudu")) return;

        switch (arguments.Split('/')) {
            case [""] or [] or null:
                System.ConfigurationWindow.IsOpen = !System.ConfigurationWindow.IsOpen;
                break;

            case ["debug"]:
                System.Config.DebugMode = !System.Config.DebugMode;
                Services.ChatGui.Print($"Debug mode is now {(System.Config.DebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Services.PluginLog.Info($"Debug mode is now {(System.Config.DebugMode ? "Enabled" : "Disabled")}");
                Task.Run(System.Config.Save);

                if (!System.ConfigurationWindow.IsOpen) {
                    System.ConfigurationWindow.IsOpen = true;
                }
                break;
        }
    }

    private static void OpenConfigUi()
        => System.ConfigurationWindow.IsOpen = !System.ConfigurationWindow.IsOpen;

    private static void OpenMainUi()
        => System.OverrideListWindow.IsOpen = !System.OverrideListWindow.IsOpen;
}
