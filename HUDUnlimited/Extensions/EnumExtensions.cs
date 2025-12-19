using System;
using System.ComponentModel;

namespace HUDUnlimited.Extensions;

public static class EnumExtensions {
    extension(Enum value) {
        public string Description => value.GetDescription();
        
        private string GetDescription() {
            var type = value.GetType();
            if (Enum.GetName(type, value) is { } name) {
                if (type.GetField(name) is { } field) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
        
            return value.ToString();
        }
    }
}
