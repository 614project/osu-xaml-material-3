using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using OsuXaml.Material3.Utilities;

namespace OsuXaml.Material3.Tests
{
    /// <summary>HCT, tonal palettes and schemes, against material-color-utilities' reference values and Material's contrast rules.</summary>
    public class ColourTests
    {
        private static readonly Colour4[] seeds =
        {
            Colour4.FromHex("6750A4"), Colour4.FromHex("0061A4"), Colour4.FromHex("006E1C"), Colour4.FromHex("B3261E"),
            Colour4.FromHex("8B5000"), Colour4.FromHex("006A6A"), Colour4.FromHex("FFDE3F"), Colour4.FromHex("FF00FF"),
            Colour4.FromHex("000000"), Colour4.FromHex("FFFFFF"),
        };

        // material-color-utilities' hct_test: red, green, blue, white and black.
        [TestCase(unchecked((int)0xFFFF0000), 27.408, 113.357, 53.241)]
        [TestCase(unchecked((int)0xFF00FF00), 142.139, 108.410, 87.737)]
        [TestCase(unchecked((int)0xFF0000FF), 282.788, 87.230, 32.302)]
        [TestCase(unchecked((int)0xFFFFFFFF), 209.492, 2.869, 100.0)]
        [TestCase(unchecked((int)0xFF000000), 0.0, 0.0, 0.0)]
        public void HctMatchesTheReference(int argb, double hue, double chroma, double tone)
        {
            var hct = Hct.FromInt(argb);

            Assert.That(hct.Hue, Is.EqualTo(hue).Within(0.01));
            Assert.That(hct.Chroma, Is.EqualTo(chroma).Within(0.01));
            Assert.That(hct.Tone, Is.EqualTo(tone).Within(0.01));
        }

        [Test]
        public void SolvingKeepsTheTone()
        {
            for (int hue = 0; hue < 360; hue += 15)
            {
                for (int chroma = 0; chroma <= 150; chroma += 25)
                {
                    for (int tone = 0; tone <= 100; tone += 10)
                    {
                        var hct = Hct.From(hue, chroma, tone);

                        // Out-of-gamut chroma is reduced to what sRGB can show; the tone stays.
                        Assert.That(hct.Tone, Is.EqualTo(tone).Within(0.5), $"hue {hue}, chroma {chroma}, tone {tone}");
                        Assert.That(hct.Chroma, Is.LessThanOrEqualTo(chroma + 3), $"hue {hue}, chroma {chroma}, tone {tone}");
                    }
                }
            }
        }

        [Test]
        public void TonalPaletteOfBlueMatchesTheReference()
        {
            // material-color-utilities' tonal_palette_test.
            var blue = Hct.FromInt(unchecked((int)0xFF0000FF));
            var palette = TonalPalette.FromHueAndChroma(blue.Hue, blue.Chroma);

            Assert.That(palette.Tone(100), Is.EqualTo(unchecked((int)0xFFFFFFFF)));
            Assert.That(palette.Tone(95), Is.EqualTo(unchecked((int)0xFFF1EFFF)));
            Assert.That(palette.Tone(90), Is.EqualTo(unchecked((int)0xFFE0E0FF)));
            Assert.That(palette.Tone(80), Is.EqualTo(unchecked((int)0xFFBEC2FF)));
            Assert.That(palette.Tone(70), Is.EqualTo(unchecked((int)0xFF9DA3FF)));
            Assert.That(palette.Tone(60), Is.EqualTo(unchecked((int)0xFF7C84FF)));
            Assert.That(palette.Tone(50), Is.EqualTo(unchecked((int)0xFF5A64FF)));
            Assert.That(palette.Tone(40), Is.EqualTo(unchecked((int)0xFF343DFF)));
            Assert.That(palette.Tone(30), Is.EqualTo(unchecked((int)0xFF0000EF)));
            Assert.That(palette.Tone(20), Is.EqualTo(unchecked((int)0xFF0001AC)));
            Assert.That(palette.Tone(10), Is.EqualTo(unchecked((int)0xFF00006E)));
            Assert.That(palette.Tone(0), Is.EqualTo(unchecked((int)0xFF000000)));
        }

        [Test]
        public void BaselineIsMaterialsPurpleScheme()
        {
            var light = ColorScheme.Baseline(false);
            var dark = ColorScheme.Baseline(true);

            Assert.That(light.Primary, Is.EqualTo(Colour4.FromHex("6750A4")));
            Assert.That(light.OnPrimary, Is.EqualTo(Colour4.FromHex("FFFFFF")));
            Assert.That(light.Surface, Is.EqualTo(Colour4.FromHex("FEF7FF")));
            Assert.That(dark.Primary, Is.EqualTo(Colour4.FromHex("D0BCFF")));
            Assert.That(dark.Surface, Is.EqualTo(Colour4.FromHex("141218")));
            Assert.That(light.IsDark, Is.False);
            Assert.That(dark.IsDark, Is.True);

            assertReadable(light, "baseline light");
            assertReadable(dark, "baseline dark");
        }

        [Test]
        public void SeedSchemesGiveEachRoleItsTone()
        {
            foreach (var seed in seeds)
            {
                foreach (var variant in Enum.GetValues<SchemeVariant>())
                {
                    var light = ColorScheme.FromSeed(seed, false, variant);
                    var dark = ColorScheme.FromSeed(seed, true, variant);
                    string what = $"{seed.ToHex()} {variant}";

                    Assert.That(tone(light.Primary), Is.EqualTo(40).Within(1), what);
                    Assert.That(tone(light.PrimaryContainer), Is.EqualTo(90).Within(1), what);
                    Assert.That(tone(light.Surface), Is.EqualTo(98).Within(1), what);
                    Assert.That(tone(dark.Primary), Is.EqualTo(80).Within(1), what);
                    Assert.That(tone(dark.PrimaryContainer), Is.EqualTo(30).Within(1), what);
                    Assert.That(tone(dark.Surface), Is.EqualTo(6).Within(1), what);

                    Assert.That(light.Seed, Is.EqualTo(seed));
                    Assert.That(light.Variant, Is.EqualTo(variant));
                }
            }
        }

        [Test]
        public void SeedSchemesAreReadable()
        {
            foreach (var seed in seeds)
            {
                foreach (var variant in Enum.GetValues<SchemeVariant>())
                {
                    assertReadable(ColorScheme.FromSeed(seed, false, variant), $"{seed.ToHex()} {variant} light");
                    assertReadable(ColorScheme.FromSeed(seed, true, variant), $"{seed.ToHex()} {variant} dark");
                }
            }
        }

        [Test]
        public void TonalSpotKeepsTheSeedsHue()
        {
            foreach (var seed in new[] { Colour4.FromHex("0061A4"), Colour4.FromHex("006E1C"), Colour4.FromHex("B3261E") })
            {
                var scheme = ColorScheme.FromSeed(seed, false);
                double difference = Math.Abs(Hct.FromColour(scheme.Primary).Hue - Hct.FromColour(seed).Hue);

                Assert.That(Math.Min(difference, 360 - difference), Is.LessThan(3), seed.ToHex());
                Assert.That(Hct.FromColour(scheme.Primary).Chroma, Is.EqualTo(36).Within(3), seed.ToHex());
            }
        }

        [Test]
        public void MonochromeIsGrey()
        {
            var scheme = ColorScheme.FromSeed(Colour4.FromHex("B3261E"), false, SchemeVariant.Monochrome);

            foreach (var role in new[] { ColorRole.Primary, ColorRole.Secondary, ColorRole.Tertiary, ColorRole.Surface, ColorRole.Outline })
            {
                var colour = scheme[role];
                Assert.That(colour.R == colour.G && colour.G == colour.B, role.ToString());
            }

            // Errors stay red whatever the variant.
            Assert.That(Hct.FromColour(scheme.Error).Chroma, Is.GreaterThan(40));
        }

        [Test]
        public void ContentRolesPairWithTheirContainers()
        {
            Assert.That(ColorScheme.ContentRoleFor(ColorRole.Primary), Is.EqualTo(ColorRole.OnPrimary));
            Assert.That(ColorScheme.ContentRoleFor(ColorRole.TertiaryContainer), Is.EqualTo(ColorRole.OnTertiaryContainer));
            Assert.That(ColorScheme.ContentRoleFor(ColorRole.SurfaceContainerHigh), Is.EqualTo(ColorRole.OnSurface));
            Assert.That(ColorScheme.ContentRoleFor(ColorRole.InverseSurface), Is.EqualTo(ColorRole.InverseOnSurface));
        }

        // Text roles on the roles they sit on reach WCAG's 4.5:1 for body text.
        private static readonly (ColorRole Background, ColorRole Content)[] text_pairs =
        {
            (ColorRole.Primary, ColorRole.OnPrimary),
            (ColorRole.PrimaryContainer, ColorRole.OnPrimaryContainer),
            (ColorRole.Secondary, ColorRole.OnSecondary),
            (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer),
            (ColorRole.Tertiary, ColorRole.OnTertiary),
            (ColorRole.TertiaryContainer, ColorRole.OnTertiaryContainer),
            (ColorRole.Error, ColorRole.OnError),
            (ColorRole.ErrorContainer, ColorRole.OnErrorContainer),
            (ColorRole.Surface, ColorRole.OnSurface),
            (ColorRole.Surface, ColorRole.OnSurfaceVariant),
            (ColorRole.SurfaceContainerHighest, ColorRole.OnSurface),
            (ColorRole.SurfaceContainerHighest, ColorRole.OnSurfaceVariant),
            (ColorRole.InverseSurface, ColorRole.InverseOnSurface),
            (ColorRole.InverseSurface, ColorRole.InversePrimary),
            (ColorRole.Surface, ColorRole.Primary),
        };

        private static void assertReadable(ColorScheme scheme, string what)
        {
            foreach (var (background, content) in text_pairs)
                Assert.That(contrast(scheme[background], scheme[content]), Is.GreaterThanOrEqualTo(4.5), $"{what}: {content} on {background}");
        }

        private static double tone(Colour4 colour) => ColorUtils.LstarFromArgb(ColorUtils.FromColour4(colour));

        private static double contrast(Colour4 a, Colour4 b)
        {
            double ya = ColorUtils.XyzFromArgb(ColorUtils.FromColour4(a))[1] / 100;
            double yb = ColorUtils.XyzFromArgb(ColorUtils.FromColour4(b))[1] / 100;
            return (Math.Max(ya, yb) + 0.05) / (Math.Min(ya, yb) + 0.05);
        }
    }
}
