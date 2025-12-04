
using HUDUnlimited.Classes;
using HUDUnlimited.Windows;
using AddonController = HUDUnlimited.Classes.AddonController;

namespace HUDUnlimited;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public static class System {
	public static Configuration Config { get; set; }
	public static AddonController AddonController { get; set; }
	// public static OverrideListWindow OverrideListWindow { get; set; }
	public static ConfigurationWindow ConfigWindow { get; set; }
}
