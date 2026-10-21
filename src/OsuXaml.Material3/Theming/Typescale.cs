using System;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>Material 3's type scale roles (md.sys.typescale).</summary>
    public enum TypeStyle
    {
        DisplayLarge,
        DisplayMedium,
        DisplaySmall,
        HeadlineLarge,
        HeadlineMedium,
        HeadlineSmall,
        TitleLarge,
        TitleMedium,
        TitleSmall,
        BodyLarge,
        BodyMedium,
        BodySmall,
        LabelLarge,
        LabelMedium,
        LabelSmall,
    }

    /// <summary>One type scale role: Roboto at a size, line height, weight and tracking, all in CSS pixels as the spec gives them.</summary>
    public readonly struct TypeToken
    {
        public TypeToken(float size, float lineHeight, int weight, float tracking)
        {
            Size = size;
            LineHeight = lineHeight;
            Weight = weight;
            Tracking = tracking;
        }

        /// <summary>The em size, as CSS font-size.</summary>
        public float Size { get; }

        public float LineHeight { get; }

        /// <summary>400 (regular) or 500 (medium).</summary>
        public int Weight { get; }

        /// <summary>Letter spacing in pixels.</summary>
        public float Tracking { get; }

        /// <summary>
        /// The osu!framework font: its size is the height of the font's line box (ascender to descender), which for Roboto is
        /// 2400/2048 of the em, so text comes out as large as on the web.
        /// </summary>
        public FontUsage Font => new FontUsage(Typescale.Family, Size * Typescale.LineBoxPerEm, Typescale.WeightName(Weight));

        /// <summary>The space between the glyphs' line box and the line height, half above and half below each line.</summary>
        public float Leading => Math.Max(0, LineHeight - Size * Typescale.LineBoxPerEm);
    }

    /// <summary>The type scale. Text controls (<see cref="Text"/> and every component's labels) take their fonts from here.</summary>
    public static class Typescale
    {
        /// <summary>Roboto's line box (hhea ascender + descender, 2400 units) over its em (2048 units).</summary>
        public const float LineBoxPerEm = 2400f / 2048f;

        /// <summary>The font family, Roboto as Material's baseline uses. A game that adds other weights of its own font can change it.</summary>
        public static string Family = "Roboto";

        private static readonly TypeToken[] tokens =
        {
            new TypeToken(57, 64, 400, -0.25f),
            new TypeToken(45, 52, 400, 0),
            new TypeToken(36, 44, 400, 0),
            new TypeToken(32, 40, 400, 0),
            new TypeToken(28, 36, 400, 0),
            new TypeToken(24, 32, 400, 0),
            new TypeToken(22, 28, 400, 0),
            new TypeToken(16, 24, 500, 0.15f),
            new TypeToken(14, 20, 500, 0.1f),
            new TypeToken(16, 24, 400, 0.5f),
            new TypeToken(14, 20, 400, 0.25f),
            new TypeToken(12, 16, 400, 0.4f),
            new TypeToken(14, 20, 500, 0.1f),
            new TypeToken(12, 16, 500, 0.5f),
            new TypeToken(11, 16, 500, 0.5f),
        };

        public static TypeToken Get(TypeStyle style) => tokens[(int)style];

        internal static string WeightName(int weight) => weight >= 700 ? "Bold" : weight >= 500 ? "Medium" : "Regular";

        /// <summary>Gives a sprite text a role's font, tracking and line height (half the leading above and below each line).</summary>
        public static void Apply(SpriteText text, TypeStyle style)
        {
            var token = Get(style);
            float leading = token.Leading;

            text.Font = token.Font;
            text.Spacing = new Vector2(token.Tracking, leading);
            text.Padding = new osu.Framework.Graphics.MarginPadding { Top = leading / 2, Bottom = leading / 2 };
        }
    }
}
