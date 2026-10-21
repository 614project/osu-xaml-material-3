using System;
using osu.Framework.Graphics;

namespace OsuXaml.Material3.Utilities
{
    /// <summary>
    /// A colour as hue, chroma and tone: Material's colour model. Hue and chroma come from CAM16 (so equal steps look equal),
    /// tone is L* (so a tone difference of 40 or more keeps text readable, whatever the hue). Ported from material-color-utilities.
    /// </summary>
    public sealed class Hct
    {
        private Hct(int argb)
        {
            var cam = Cam16.FromInt(argb);
            Hue = cam.Hue;
            Chroma = cam.Chroma;
            Tone = ColorUtils.LstarFromArgb(argb);
            Argb = argb;
        }

        /// <summary>0 to 360.</summary>
        public double Hue { get; }

        /// <summary>0 to about 150; how far from grey. What can be reached depends on hue and tone.</summary>
        public double Chroma { get; }

        /// <summary>0 (black) to 100 (white).</summary>
        public double Tone { get; }

        public int Argb { get; }

        public Colour4 ToColour4() => ColorUtils.ToColour4(Argb);

        public static Hct FromInt(int argb) => new Hct(argb);

        public static Hct FromColour(Colour4 colour) => new Hct(ColorUtils.FromColour4(colour));

        /// <summary>The colour with this hue and tone and the chroma asked for, or the most chroma sRGB has for them.</summary>
        public static Hct From(double hue, double chroma, double tone) => new Hct(SolveToInt(hue, chroma, tone));

        public Hct WithTone(double tone) => From(Hue, Chroma, tone);

        public override string ToString() => $"H{Hue:0.##} C{Chroma:0.##} T{Tone:0.##} #{Argb & 0xFFFFFF:X6}";

        /// <summary>The colour with this hue and tone and the chroma asked for, or the most chroma sRGB has for them, as ARGB.</summary>
        public static int SolveToInt(double hue, double chroma, double lstar) => HctSolver.SolveToInt(hue, chroma, lstar);
    }
}
