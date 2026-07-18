using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;

namespace HUDUnlimited;

public sealed class HUDUnlimitedPlugin : IAsyncDalamudPlugin {
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    public Task LoadAsync(CancellationToken cancellationToken) {
        System.Config = Configuration.Load();

        System.ConfigurationWindow = new ConfigurationWindow();
        System.OverrideListWindow = new OverrideListWindow();

        System.AddonController = new AddonController();

        System.WindowSystem = new WindowSystem("HUDUnlimited");
        System.WindowSystem.AddWindow(System.ConfigurationWindow);
        System.WindowSystem.AddWindow(System.OverrideListWindow);

        ICommandManager.Get().AddHandler("/hudunlimited", new CommandInfo(OnCommand) {
            HelpMessage = "Open HUDUnlimited Config Window",
            ShowInHelp = true,
        });

        ICommandManager.Get().AddHandler("/hudu", new CommandInfo(OnCommand) {
            HelpMessage = "Open HUDUnlimited Config Window",
            ShowInHelp = true,
        });

        PluginInterface.UiBuilder.Draw += System.WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

        if (System.Config.DebugMode) {
            System.ConfigurationWindow.IsOpen = true;
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() {
        PluginInterface.UiBuilder.OpenConfigUi -= System.WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;

        System.WindowSystem.RemoveAllWindows();

        ICommandManager.Get().RemoveHandler("/hudunlimited");
        ICommandManager.Get().RemoveHandler("/hudu");

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
                IChatGui.Get().Print($"Debug mode is now {(System.Config.DebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                IPluginLog.Get().Info($"Debug mode is now {(System.Config.DebugMode ? "Enabled" : "Disabled")}");
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
