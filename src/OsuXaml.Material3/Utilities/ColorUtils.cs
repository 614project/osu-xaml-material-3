using System;
using osu.Framework.Graphics;

namespace OsuXaml.Material3.Utilities
{
    /// <summary>
    /// Colour space conversions for the HCT colour model, ported from Google's material-color-utilities.
    /// Colours are packed ARGB integers (0xAARRGGBB), as the reference implementation uses; linear RGB components run 0 to 100.
    /// </summary>
    public static class ColorUtils
    {
        internal static readonly double[][] SRGB_TO_XYZ =
        {
            new[] { 0.41233895, 0.35762064, 0.18051042 },
            new[] { 0.2126, 0.7152, 0.0722 },
            new[] { 0.01932141, 0.11916382, 0.95034478 },
        };

        internal static readonly double[][] XYZ_TO_SRGB =
        {
            new[] { 3.2413774792388685, -1.5376652402851851, -0.49885366846268053 },
            new[] { -0.9691452513005321, 1.8758853451067872, 0.04156585616912061 },
            new[] { 0.05562093689691305, -0.20395524564742123, 1.0571799111220335 },
        };

        /// <summary>D65, the white point HCT is defined under.</summary>
        internal static readonly double[] WHITE_POINT_D65 = { 95.047, 100.0, 108.883 };

        public static int ArgbFromRgb(int red, int green, int blue) => (255 << 24) | ((red & 255) << 16) | ((green & 255) << 8) | (blue & 255);

        public static int ArgbFromLinrgb(double[] linrgb) => ArgbFromRgb(Delinearized(linrgb[0]), Delinearized(linrgb[1]), Delinearized(linrgb[2]));

        public static int AlphaFromArgb(int argb) => (argb >> 24) & 255;

        public static int RedFromArgb(int argb) => (argb >> 16) & 255;

        public static int GreenFromArgb(int argb) => (argb >> 8) & 255;

        public static int BlueFromArgb(int argb) => argb & 255;

        public static int ArgbFromXyz(double x, double y, double z)
        {
            var m = XYZ_TO_SRGB;
            double linearR = m[0][0] * x + m[0][1] * y + m[0][2] * z;
            double linearG = m[1][0] * x + m[1][1] * y + m[1][2] * z;
            double linearB = m[2][0] * x + m[2][1] * y + m[2][2] * z;
            return ArgbFromRgb(Delinearized(linearR), Delinearized(linearG), Delinearized(linearB));
        }

        public static double[] XyzFromArgb(int argb)
        {
            double r = Linearized(RedFromArgb(argb));
            double g = Linearized(GreenFromArgb(argb));
            double b = Linearized(BlueFromArgb(argb));
            var m = SRGB_TO_XYZ;
            return new[]
            {
                m[0][0] * r + m[0][1] * g + m[0][2] * b,
                m[1][0] * r + m[1][1] * g + m[1][2] * b,
                m[2][0] * r + m[2][1] * g + m[2][2] * b,
            };
        }

        /// <summary>The grey with the given L* (0 to 100).</summary>
        public static int ArgbFromLstar(double lstar)
        {
            int component = Delinearized(YFromLstar(lstar));
            return ArgbFromRgb(component, component, component);
        }

        /// <summary>The L* (perceptual lightness, 0 to 100) of a colour; HCT's tone.</summary>
        public static double LstarFromArgb(int argb) => 116.0 * labF(XyzFromArgb(argb)[1] / 100.0) - 16.0;

        /// <summary>Relative luminance Y (0 to 100) from L*.</summary>
        public static double YFromLstar(double lstar) => 100.0 * labInvf((lstar + 16.0) / 116.0);

        public static double LstarFromY(double y) => labF(y / 100.0) * 116.0 - 16.0;

        /// <summary>An sRGB component (0 to 255) to linear RGB (0 to 100).</summary>
        public static double Linearized(int rgbComponent)
        {
            double normalized = rgbComponent / 255.0;
            return normalized <= 0.040449936 ? normalized / 12.92 * 100.0 : Math.Pow((normalized + 0.055) / 1.055, 2.4) * 100.0;
        }

        /// <summary>A linear RGB component (0 to 100) to sRGB (0 to 255), rounded and clamped.</summary>
        public static int Delinearized(double rgbComponent)
        {
            double normalized = rgbComponent / 100.0;
            double delinearized = normalized <= 0.0031308 ? normalized * 12.92 : 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;
            // Rounded half up, as Java's Math.round in the reference.
            return Math.Clamp((int)Math.Floor(delinearized * 255.0 + 0.5), 0, 255);
        }

        private static double labF(double t)
        {
            const double e = 216.0 / 24389.0;
            const double kappa = 24389.0 / 27.0;
            return t > e ? Math.Cbrt(t) : (kappa * t + 16) / 116;
        }

        private static double labInvf(double ft)
        {
            const double e = 216.0 / 24389.0;
            const double kappa = 24389.0 / 27.0;
            double ft3 = ft * ft * ft;
            return ft3 > e ? ft3 : (116 * ft - 16) / kappa;
        }

        public static Colour4 ToColour4(int argb) => new Colour4((byte)RedFromArgb(argb), (byte)GreenFromArgb(argb), (byte)BlueFromArgb(argb), (byte)AlphaFromArgb(argb));

        public static int FromColour4(Colour4 colour)
            => (byte255(colour.A) << 24) | (byte255(colour.R) << 16) | (byte255(colour.G) << 8) | byte255(colour.B);

        private static int byte255(float component) => Math.Clamp((int)Math.Round(component * 255), 0, 255);
    }

    internal static class MathUtils
    {
        public static double SanitizeDegrees(double degrees)
        {
            degrees %= 360.0;
            return degrees < 0 ? degrees + 360.0 : degrees;
        }

        public static double Lerp(double start, double stop, double amount) => (1.0 - amount) * start + amount * stop;

        public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
    }
}
