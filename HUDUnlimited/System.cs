using Dalamud.Interface.Windowing;
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;

namespace HUDUnlimited;

public static class System {
    public static Configuration Config { get; set; } = null!;
	public static ConfigurationWindow ConfigurationWindow { get; set; } = null!;
	public static WindowSystem WindowSystem { get; set; } = null!;
	public static AddonController AddonController { get; set; } = null!;
	public static OverrideListWindow OverrideListWindow { get; set; } = null!;
}
