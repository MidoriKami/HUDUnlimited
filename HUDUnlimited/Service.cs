using Dalamud.IoC;
using Dalamud.Plugin;

namespace HUDUnlimited;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class Service {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
}