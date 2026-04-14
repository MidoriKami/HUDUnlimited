using System.Diagnostics;
using Dalamud.Interface.Windowing;

namespace HUDUnlimited.Extensions;

public static class WindowExtensions {
    extension(Window window) {
        [Conditional("DEBUG")]
        public void DebugOpen()
            => window.IsOpen = true;
    }
}
