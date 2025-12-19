using System.Collections.Generic;
using ChillFrames.Utilities;

namespace HUDUnlimited.Classes;

public class Configuration {
    public bool HideInactiveAddons = false;
    public bool HideInactiveNodes = false;
    public List<OverrideConfig> Overrides = [];
    
    public static Configuration Load()
        => Config.LoadConfig<Configuration>("System.config.json");
    
    public void Save() 
        => Config.SaveConfig(this, "System.config.json");
}
