using System.Collections.Generic;
using KamiLib.Configuration;

namespace HUDUnlimited.Classes;

public class Configuration {
    public bool HideInactiveAddons = false;
    public bool HideInactiveNodes = false;
    public List<OverrideConfig> Overrides = [];
    
    public static Configuration Load()
        => Service.PluginInterface.LoadConfigFile<Configuration>("System.config.json");
    
    public void Save() 
        => Service.PluginInterface.SaveConfigFile("System.config.json", this);
}