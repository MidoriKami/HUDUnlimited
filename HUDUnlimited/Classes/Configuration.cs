using KamiLib.Configuration;

namespace HUDUnlimited.Classes;

public class Configuration {
    public static Configuration Load()
        => Service.PluginInterface.LoadConfigFile("System.config.json", () => new Configuration());
    
    public void Save() 
        => Service.PluginInterface.SaveConfigFile("System.config.json", this);
}