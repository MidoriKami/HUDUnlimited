using System.Collections.Generic;

namespace HUDUnlimited.Classes;

public class Configuration {
    public List<OverrideConfig> Overrides = [];

    public static Configuration Load()
        => new();

    public void Save() { } // Don't save while in development mode
}
