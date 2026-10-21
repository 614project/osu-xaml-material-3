using System.Collections.Generic;
using osu.Framework.Graphics;

namespace OsuXaml.Material3.Utilities
{
    /// <summary>
    /// Every tone (0 to 100) of one hue and chroma. Colour schemes take their roles from a handful of these:
    /// primary is tone 40 of the primary palette in light themes and tone 80 in dark ones, and so on.
    /// </summary>
    public sealed class TonalPalette
    {
        private readonly Dictionary<int, int> cache = new Dictionary<int, int>();

        private TonalPalette(double hue, double chroma)
        {
            Hue = hue;
            Chroma = chroma;
        }

        public double Hue { get; }

        public double Chroma { get; }

        public static TonalPalette FromHueAndChroma(double hue, double chroma) => new TonalPalette(hue, chroma);

        /// <summary>The palette of a colour's own hue and chroma.</summary>
        public static TonalPalette FromColour(Colour4 colour)
        {
            var hct = Hct.FromColour(colour);
            return new TonalPalette(hct.Hue, hct.Chroma);
        }

        /// <summary>The ARGB colour at a tone (0 to 100).</summary>
        public int Tone(int tone)
        {
            lock (cache)
            {
                if (!cache.TryGetValue(tone, out int argb))
                    cache[tone] = argb = Hct.SolveToInt(Hue, Chroma, tone);

                return argb;
            }
        }

        public Colour4 this[int tone] => ColorUtils.ToColour4(Tone(tone));
    }
}
