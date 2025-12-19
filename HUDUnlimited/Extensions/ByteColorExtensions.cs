using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace HUDUnlimited.Extensions;

public static class ByteColorExtensions {
    extension(ref ByteColor byteColor) {
        public Vector4 ToVector4() => new Vector4(byteColor.R, byteColor.G, byteColor.B, byteColor.A) / 255.0f;
    }
}
