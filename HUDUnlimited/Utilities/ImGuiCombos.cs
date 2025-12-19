using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HUDUnlimited.Extensions;

namespace HUDUnlimited.Utilities;

public static class ImGuiCombos {
    public static bool EnumFlagCombo<T>(string label, ref T refValue) where T : Enum {
        using var combo = ImRaii.Combo(label, refValue.Description);
        if (!combo) return false;

        foreach (Enum enumValue in Enum.GetValues(refValue.GetType())) {
            if (ImGui.Selectable(enumValue.Description, refValue.HasFlag(enumValue))) {
                if (!refValue.HasFlag(enumValue)) {
                    var intRefValue = Convert.ToInt32(refValue);
                    var intFlagValue = Convert.ToInt32(enumValue);
                    var result = intRefValue | intFlagValue;
                    refValue = (T)Enum.ToObject(refValue.GetType(), result);
                }
                else {
                    var intRefValue = Convert.ToInt32(refValue);
                    var intFlagValue = Convert.ToInt32(enumValue);
                    var result = intRefValue & ~intFlagValue;
                    refValue = (T)Enum.ToObject(refValue.GetType(), result);
                }

                return true;
            }
        }

        return false;
    }
    
    public static bool EnumCombo<T>(string label, ref T refValue) where T : Enum {
        using var combo = ImRaii.Combo(label, refValue.Description);
        if (!combo) return false;

        foreach (Enum enumValue in Enum.GetValues(refValue.GetType())) {
            if (!ImGui.Selectable(enumValue.Description, enumValue.Equals(refValue))) continue;
            
            refValue = (T)enumValue;
            return true;
        }

        return false;
    }
}
