using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>Material 3's corner scale (md.sys.shape.corner).</summary>
    public enum ShapeCorner
    {
        None,

        /// <summary>4dp: text fields, menus, snackbars.</summary>
        ExtraSmall,

        /// <summary>8dp: chips.</summary>
        Small,

        /// <summary>12dp: cards, small FABs.</summary>
        Medium,

        /// <summary>16dp: FABs, navigation drawers.</summary>
        Large,

        /// <summary>28dp: dialogs, sheets, large FABs.</summary>
        ExtraLarge,

        /// <summary>Half the shorter side: buttons, badges, the navigation indicator.</summary>
        Full,
    }

    public static class Shape
    {
        public static float Radius(ShapeCorner corner, Vector2 size) => corner switch
        {
            ShapeCorner.None => 0,
            ShapeCorner.ExtraSmall => 4,
            ShapeCorner.Small => 8,
            ShapeCorner.Medium => 12,
            ShapeCorner.Large => 16,
            ShapeCorner.ExtraLarge => 28,
            _ => Math.Min(size.X, size.Y) / 2,
        };
    }

    /// <summary>Material 3's elevation levels (md.sys.elevation): 0, 1, 3, 6, 8 and 12dp.</summary>
    public static class Elevation
    {
        public const int Level0 = 0;
        public const int Level1 = 1;
        public const int Level2 = 2;
        public const int Level3 = 3;
        public const int Level4 = 4;
        public const int Level5 = 5;

        // Material's two shadows per level (the key light below, the ambient light around), as CSS box-shadows: y offset, blur, spread.
        private static readonly (float Y, float Blur, float Spread)[] key = { (0, 0, 0), (1, 2, 0), (1, 2, 0), (1, 3, 0), (2, 3, 0), (4, 4, 0) };
        private static readonly (float Y, float Blur, float Spread)[] ambient = { (0, 0, 0), (1, 3, 1), (2, 6, 2), (4, 8, 3), (6, 10, 4), (8, 12, 6) };

        /// <summary>The key shadow (30% black) of a level, as an osu!framework edge effect.</summary>
        public static EdgeEffectParameters KeyShadow(int level, Colour4 shadow) => effect(key[Math.Clamp(level, 0, 5)], shadow.Opacity(0.3f));

        /// <summary>The ambient shadow (15% black) of a level, without its spread (see <see cref="AmbientSpread"/>).</summary>
        public static EdgeEffectParameters AmbientShadow(int level, Colour4 shadow) => effect(ambient[Math.Clamp(level, 0, 5)], shadow.Opacity(0.15f));

        /// <summary>How far the ambient shadow's shape grows past the surface.</summary>
        public static float AmbientSpread(int level) => ambient[Math.Clamp(level, 0, 5)].Spread;

        private static EdgeEffectParameters effect((float Y, float Blur, float Spread) s, Colour4 colour) => new EdgeEffectParameters
        {
            Type = EdgeEffectType.Shadow,
            Colour = s.Blur > 0 ? colour : Colour4.Transparent,
            Offset = new Vector2(0, s.Y),
            Radius = s.Blur,
            Hollow = true,
        };
    }

    /// <summary>Opacities of Material 3's state layers (md.sys.state) and of disabled content.</summary>
    public static class StateOpacity
    {
        public const float Hover = 0.08f;
        public const float Focus = 0.10f;
        public const float Pressed = 0.10f;
        public const float Dragged = 0.16f;

        public const float DisabledContainer = 0.12f;
        public const float DisabledContent = 0.38f;
    }
}
