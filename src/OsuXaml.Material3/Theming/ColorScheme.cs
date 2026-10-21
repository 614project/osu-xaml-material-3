using System;
using osu.Framework.Graphics;
using OsuXaml.Material3.Utilities;

namespace OsuXaml.Material3
{
    /// <summary>Material 3's colour roles. Components ask for roles, never fixed colours, so any scheme restyles them.</summary>
    public enum ColorRole
    {
        Primary,
        OnPrimary,
        PrimaryContainer,
        OnPrimaryContainer,
        InversePrimary,

        Secondary,
        OnSecondary,
        SecondaryContainer,
        OnSecondaryContainer,

        Tertiary,
        OnTertiary,
        TertiaryContainer,
        OnTertiaryContainer,

        Error,
        OnError,
        ErrorContainer,
        OnErrorContainer,

        Surface,
        OnSurface,
        SurfaceVariant,
        OnSurfaceVariant,
        SurfaceDim,
        SurfaceBright,
        SurfaceContainerLowest,
        SurfaceContainerLow,
        SurfaceContainer,
        SurfaceContainerHigh,
        SurfaceContainerHighest,
        InverseSurface,
        InverseOnSurface,
        SurfaceTint,

        Background,
        OnBackground,

        Outline,
        OutlineVariant,
        Shadow,
        Scrim,

        PrimaryFixed,
        PrimaryFixedDim,
        OnPrimaryFixed,
        OnPrimaryFixedVariant,
        SecondaryFixed,
        SecondaryFixedDim,
        OnSecondaryFixed,
        OnSecondaryFixedVariant,
        TertiaryFixed,
        TertiaryFixedDim,
        OnTertiaryFixed,
        OnTertiaryFixedVariant,
    }

    /// <summary>How a scheme made from a seed colour spreads its palettes, as Material's dynamic colour does.</summary>
    public enum SchemeVariant
    {
        /// <summary>The default: a calm primary of the seed's hue, a muted secondary and a tertiary 60° around the wheel.</summary>
        TonalSpot,

        /// <summary>Almost grey, with a hint of the seed's hue.</summary>
        Neutral,

        /// <summary>The most colourful primary the hue allows, with secondary and tertiary hues nudged along.</summary>
        Vibrant,

        /// <summary>A primary away from the seed's hue, for playful themes.</summary>
        Expressive,

        /// <summary>Greys only.</summary>
        Monochrome,
    }

    /// <summary>
    /// A colour for every <see cref="ColorRole"/>. <see cref="Baseline"/> is Material 3's purple baseline scheme;
    /// <see cref="FromSeed"/> makes a scheme from any colour with HCT tonal palettes, as Material You's dynamic colour does.
    /// </summary>
    public sealed class ColorScheme
    {
        private static readonly int role_count = Enum.GetValues<ColorRole>().Length;

        private readonly Colour4[] colours;

        private ColorScheme(bool isDark, Func<ColorRole, int> argb)
        {
            IsDark = isDark;
            colours = new Colour4[role_count];

            for (int i = 0; i < role_count; i++)
                colours[i] = ColorUtils.ToColour4(argb((ColorRole)i));
        }

        public bool IsDark { get; }

        /// <summary>The seed the scheme was made from, or null for <see cref="Baseline"/>.</summary>
        public Colour4? Seed { get; private init; }

        public SchemeVariant Variant { get; private init; }

        public Colour4 this[ColorRole role] => colours[(int)role];

        public Colour4 Primary => this[ColorRole.Primary];
        public Colour4 OnPrimary => this[ColorRole.OnPrimary];
        public Colour4 PrimaryContainer => this[ColorRole.PrimaryContainer];
        public Colour4 OnPrimaryContainer => this[ColorRole.OnPrimaryContainer];
        public Colour4 InversePrimary => this[ColorRole.InversePrimary];
        public Colour4 Secondary => this[ColorRole.Secondary];
        public Colour4 OnSecondary => this[ColorRole.OnSecondary];
        public Colour4 SecondaryContainer => this[ColorRole.SecondaryContainer];
        public Colour4 OnSecondaryContainer => this[ColorRole.OnSecondaryContainer];
        public Colour4 Tertiary => this[ColorRole.Tertiary];
        public Colour4 OnTertiary => this[ColorRole.OnTertiary];
        public Colour4 TertiaryContainer => this[ColorRole.TertiaryContainer];
        public Colour4 OnTertiaryContainer => this[ColorRole.OnTertiaryContainer];
        public Colour4 Error => this[ColorRole.Error];
        public Colour4 OnError => this[ColorRole.OnError];
        public Colour4 ErrorContainer => this[ColorRole.ErrorContainer];
        public Colour4 OnErrorContainer => this[ColorRole.OnErrorContainer];
        public Colour4 Surface => this[ColorRole.Surface];
        public Colour4 OnSurface => this[ColorRole.OnSurface];
        public Colour4 SurfaceVariant => this[ColorRole.SurfaceVariant];
        public Colour4 OnSurfaceVariant => this[ColorRole.OnSurfaceVariant];
        public Colour4 SurfaceDim => this[ColorRole.SurfaceDim];
        public Colour4 SurfaceBright => this[ColorRole.SurfaceBright];
        public Colour4 SurfaceContainerLowest => this[ColorRole.SurfaceContainerLowest];
        public Colour4 SurfaceContainerLow => this[ColorRole.SurfaceContainerLow];
        public Colour4 SurfaceContainer => this[ColorRole.SurfaceContainer];
        public Colour4 SurfaceContainerHigh => this[ColorRole.SurfaceContainerHigh];
        public Colour4 SurfaceContainerHighest => this[ColorRole.SurfaceContainerHighest];
        public Colour4 InverseSurface => this[ColorRole.InverseSurface];
        public Colour4 InverseOnSurface => this[ColorRole.InverseOnSurface];
        public Colour4 SurfaceTint => this[ColorRole.SurfaceTint];
        public Colour4 Background => this[ColorRole.Background];
        public Colour4 OnBackground => this[ColorRole.OnBackground];
        public Colour4 Outline => this[ColorRole.Outline];
        public Colour4 OutlineVariant => this[ColorRole.OutlineVariant];
        public Colour4 Shadow => this[ColorRole.Shadow];
        public Colour4 Scrim => this[ColorRole.Scrim];
        public Colour4 PrimaryFixed => this[ColorRole.PrimaryFixed];
        public Colour4 PrimaryFixedDim => this[ColorRole.PrimaryFixedDim];
        public Colour4 OnPrimaryFixed => this[ColorRole.OnPrimaryFixed];
        public Colour4 OnPrimaryFixedVariant => this[ColorRole.OnPrimaryFixedVariant];
        public Colour4 SecondaryFixed => this[ColorRole.SecondaryFixed];
        public Colour4 SecondaryFixedDim => this[ColorRole.SecondaryFixedDim];
        public Colour4 OnSecondaryFixed => this[ColorRole.OnSecondaryFixed];
        public Colour4 OnSecondaryFixedVariant => this[ColorRole.OnSecondaryFixedVariant];
        public Colour4 TertiaryFixed => this[ColorRole.TertiaryFixed];
        public Colour4 TertiaryFixedDim => this[ColorRole.TertiaryFixedDim];
        public Colour4 OnTertiaryFixed => this[ColorRole.OnTertiaryFixed];
        public Colour4 OnTertiaryFixedVariant => this[ColorRole.OnTertiaryFixedVariant];

        /// <summary>The colour for content on a role's colour: OnPrimary on Primary, OnSurface on any surface, and so on.</summary>
        public static ColorRole ContentRoleFor(ColorRole container) => container switch
        {
            ColorRole.Primary => ColorRole.OnPrimary,
            ColorRole.PrimaryContainer => ColorRole.OnPrimaryContainer,
            ColorRole.Secondary => ColorRole.OnSecondary,
            ColorRole.SecondaryContainer => ColorRole.OnSecondaryContainer,
            ColorRole.Tertiary => ColorRole.OnTertiary,
            ColorRole.TertiaryContainer => ColorRole.OnTertiaryContainer,
            ColorRole.Error => ColorRole.OnError,
            ColorRole.ErrorContainer => ColorRole.OnErrorContainer,
            ColorRole.InverseSurface => ColorRole.InverseOnSurface,
            ColorRole.SurfaceVariant => ColorRole.OnSurfaceVariant,
            ColorRole.Background => ColorRole.OnBackground,
            ColorRole.PrimaryFixed or ColorRole.PrimaryFixedDim => ColorRole.OnPrimaryFixed,
            ColorRole.SecondaryFixed or ColorRole.SecondaryFixedDim => ColorRole.OnSecondaryFixed,
            ColorRole.TertiaryFixed or ColorRole.TertiaryFixedDim => ColorRole.OnTertiaryFixed,
            _ => ColorRole.OnSurface,
        };

        #region Baseline

        private static readonly int[] baseline_light =
        {
            0x6750A4, 0xFFFFFF, 0xEADDFF, 0x21005D, 0xD0BCFF,
            0x625B71, 0xFFFFFF, 0xE8DEF8, 0x1D192B,
            0x7D5260, 0xFFFFFF, 0xFFD8E4, 0x31111D,
            0xB3261E, 0xFFFFFF, 0xF9DEDC, 0x410E0B,
            0xFEF7FF, 0x1D1B20, 0xE7E0EC, 0x49454F, 0xDED8E1, 0xFEF7FF,
            0xFFFFFF, 0xF7F2FA, 0xF3EDF7, 0xECE6F0, 0xE6E0E9,
            0x322F35, 0xF5EFF7, 0x6750A4,
            0xFEF7FF, 0x1D1B20,
            0x79747E, 0xCAC4D0, 0x000000, 0x000000,
            0xEADDFF, 0xD0BCFF, 0x21005D, 0x4F378B,
            0xE8DEF8, 0xCCC2DC, 0x1D192B, 0x4A4458,
            0xFFD8E4, 0xEFB8C8, 0x31111D, 0x633B48,
        };

        private static readonly int[] baseline_dark =
        {
            0xD0BCFF, 0x381E72, 0x4F378B, 0xEADDFF, 0x6750A4,
            0xCCC2DC, 0x332D41, 0x4A4458, 0xE8DEF8,
            0xEFB8C8, 0x492532, 0x633B48, 0xFFD8E4,
            0xF2B8B5, 0x601410, 0x8C1D18, 0xF9DEDC,
            0x141218, 0xE6E0E9, 0x49454F, 0xCAC4D0, 0x141218, 0x3B383E,
            0x0F0D13, 0x1D1B20, 0x211F26, 0x2B2930, 0x36343B,
            0xE6E0E9, 0x322F35, 0xD0BCFF,
            0x141218, 0xE6E0E9,
            0x938F99, 0x49454F, 0x000000, 0x000000,
            0xEADDFF, 0xD0BCFF, 0x21005D, 0x4F378B,
            0xE8DEF8, 0xCCC2DC, 0x1D192B, 0x4A4458,
            0xFFD8E4, 0xEFB8C8, 0x31111D, 0x633B48,
        };

        private static readonly ColorScheme baseline_light_scheme = new ColorScheme(false, r => unchecked((int)0xFF000000) | baseline_light[(int)r]);
        private static readonly ColorScheme baseline_dark_scheme = new ColorScheme(true, r => unchecked((int)0xFF000000) | baseline_dark[(int)r]);

        /// <summary>Material 3's baseline scheme (the purple one in its documentation).</summary>
        public static ColorScheme Baseline(bool dark) => dark ? baseline_dark_scheme : baseline_light_scheme;

        #endregion

        #region Dynamic colour

        /// <summary>
        /// A scheme from a seed colour: five HCT tonal palettes in the <paramref name="variant"/>'s chromas, with each role taking
        /// the tone Material assigns it in light or dark themes.
        /// </summary>
        public static ColorScheme FromSeed(Colour4 seed, bool dark, SchemeVariant variant = SchemeVariant.TonalSpot)
        {
            var source = Hct.FromColour(seed);
            var p = Palettes.For(source, variant);

            return new ColorScheme(dark, role => roleColour(p, role, dark))
            {
                Seed = seed,
                Variant = variant,
            };
        }

        /// <summary>The tonal palettes a seed and variant make, for roles of your own.</summary>
        public sealed class Palettes
        {
            private static readonly double[] vibrant_hues = { 0, 41, 61, 101, 131, 181, 251, 301, 360 };
            private static readonly double[] vibrant_secondary = { 18, 15, 10, 12, 15, 18, 15, 12, 12 };
            private static readonly double[] vibrant_tertiary = { 35, 30, 20, 25, 30, 35, 30, 25, 25 };
            private static readonly double[] expressive_hues = { 0, 21, 51, 121, 151, 191, 271, 321, 360 };
            private static readonly double[] expressive_secondary = { 45, 95, 45, 20, 45, 90, 45, 45, 45 };
            private static readonly double[] expressive_tertiary = { 120, 120, 20, 45, 20, 15, 20, 120, 120 };

            public TonalPalette Primary { get; private init; }
            public TonalPalette Secondary { get; private init; }
            public TonalPalette Tertiary { get; private init; }
            public TonalPalette Neutral { get; private init; }
            public TonalPalette NeutralVariant { get; private init; }
            public TonalPalette Error { get; } = TonalPalette.FromHueAndChroma(25, 84);

            public static Palettes For(Hct source, SchemeVariant variant)
            {
                double h = source.Hue;

                switch (variant)
                {
                    case SchemeVariant.Neutral:
                        return make(h, 12, h, 8, h, 16, h, 2, h, 2);

                    case SchemeVariant.Vibrant:
                        return make(h, 200, rotated(h, vibrant_hues, vibrant_secondary), 24, rotated(h, vibrant_hues, vibrant_tertiary), 32, h, 10, h, 12);

                    case SchemeVariant.Expressive:
                        return make(h + 240, 40, rotated(h, expressive_hues, expressive_secondary), 24, rotated(h, expressive_hues, expressive_tertiary), 32,
                            h + 15, 8, h + 15, 12);

                    case SchemeVariant.Monochrome:
                        return make(h, 0, h, 0, h, 0, h, 0, h, 0);

                    default:
                        return make(h, 36, h, 16, h + 60, 24, h, 6, h, 8);
                }
            }

            private static Palettes make(double ph, double pc, double sh, double sc, double th, double tc, double nh, double nc, double vh, double vc) => new Palettes
            {
                Primary = TonalPalette.FromHueAndChroma(MathUtils.SanitizeDegrees(ph), pc),
                Secondary = TonalPalette.FromHueAndChroma(MathUtils.SanitizeDegrees(sh), sc),
                Tertiary = TonalPalette.FromHueAndChroma(MathUtils.SanitizeDegrees(th), tc),
                Neutral = TonalPalette.FromHueAndChroma(MathUtils.SanitizeDegrees(nh), nc),
                NeutralVariant = TonalPalette.FromHueAndChroma(MathUtils.SanitizeDegrees(vh), vc),
            };

            /// <summary>The seed's hue turned by the rotation for the range it falls in.</summary>
            private static double rotated(double hue, double[] hues, double[] rotations)
            {
                for (int i = 0; i < hues.Length - 1; i++)
                {
                    if (hues[i] < hue && hue < hues[i + 1])
                        return MathUtils.SanitizeDegrees(hue + rotations[i]);
                }

                return hue;
            }
        }

        private static int roleColour(Palettes p, ColorRole role, bool dark)
        {
            int t(int light, int darkTone) => dark ? darkTone : light;

            return role switch
            {
                ColorRole.Primary => p.Primary.Tone(t(40, 80)),
                ColorRole.OnPrimary => p.Primary.Tone(t(100, 20)),
                ColorRole.PrimaryContainer => p.Primary.Tone(t(90, 30)),
                ColorRole.OnPrimaryContainer => p.Primary.Tone(t(10, 90)),
                ColorRole.InversePrimary => p.Primary.Tone(t(80, 40)),
                ColorRole.Secondary => p.Secondary.Tone(t(40, 80)),
                ColorRole.OnSecondary => p.Secondary.Tone(t(100, 20)),
                ColorRole.SecondaryContainer => p.Secondary.Tone(t(90, 30)),
                ColorRole.OnSecondaryContainer => p.Secondary.Tone(t(10, 90)),
                ColorRole.Tertiary => p.Tertiary.Tone(t(40, 80)),
                ColorRole.OnTertiary => p.Tertiary.Tone(t(100, 20)),
                ColorRole.TertiaryContainer => p.Tertiary.Tone(t(90, 30)),
                ColorRole.OnTertiaryContainer => p.Tertiary.Tone(t(10, 90)),
                ColorRole.Error => p.Error.Tone(t(40, 80)),
                ColorRole.OnError => p.Error.Tone(t(100, 20)),
                ColorRole.ErrorContainer => p.Error.Tone(t(90, 30)),
                ColorRole.OnErrorContainer => p.Error.Tone(t(10, 90)),
                ColorRole.Surface or ColorRole.Background => p.Neutral.Tone(t(98, 6)),
                ColorRole.OnSurface or ColorRole.OnBackground => p.Neutral.Tone(t(10, 90)),
                ColorRole.SurfaceVariant => p.NeutralVariant.Tone(t(90, 30)),
                ColorRole.OnSurfaceVariant => p.NeutralVariant.Tone(t(30, 80)),
                ColorRole.SurfaceDim => p.Neutral.Tone(t(87, 6)),
                ColorRole.SurfaceBright => p.Neutral.Tone(t(98, 24)),
                ColorRole.SurfaceContainerLowest => p.Neutral.Tone(t(100, 4)),
                ColorRole.SurfaceContainerLow => p.Neutral.Tone(t(96, 10)),
                ColorRole.SurfaceContainer => p.Neutral.Tone(t(94, 12)),
                ColorRole.SurfaceContainerHigh => p.Neutral.Tone(t(92, 17)),
                ColorRole.SurfaceContainerHighest => p.Neutral.Tone(t(90, 22)),
                ColorRole.InverseSurface => p.Neutral.Tone(t(20, 90)),
                ColorRole.InverseOnSurface => p.Neutral.Tone(t(95, 20)),
                ColorRole.SurfaceTint => p.Primary.Tone(t(40, 80)),
                ColorRole.Outline => p.NeutralVariant.Tone(t(50, 60)),
                ColorRole.OutlineVariant => p.NeutralVariant.Tone(t(80, 30)),
                ColorRole.Shadow or ColorRole.Scrim => p.Neutral.Tone(0),
                ColorRole.PrimaryFixed => p.Primary.Tone(90),
                ColorRole.PrimaryFixedDim => p.Primary.Tone(80),
                ColorRole.OnPrimaryFixed => p.Primary.Tone(10),
                ColorRole.OnPrimaryFixedVariant => p.Primary.Tone(30),
                ColorRole.SecondaryFixed => p.Secondary.Tone(90),
                ColorRole.SecondaryFixedDim => p.Secondary.Tone(80),
                ColorRole.OnSecondaryFixed => p.Secondary.Tone(10),
                ColorRole.OnSecondaryFixedVariant => p.Secondary.Tone(30),
                ColorRole.TertiaryFixed => p.Tertiary.Tone(90),
                ColorRole.TertiaryFixedDim => p.Tertiary.Tone(80),
                ColorRole.OnTertiaryFixed => p.Tertiary.Tone(10),
                ColorRole.OnTertiaryFixedVariant => p.Tertiary.Tone(30),
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
        }

        #endregion
    }
}
