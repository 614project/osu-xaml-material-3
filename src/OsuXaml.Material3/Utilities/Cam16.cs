using System;

namespace OsuXaml.Material3.Utilities
{
    /// <summary>The environment a colour is seen in, which CAM16 adapts to. <see cref="Default"/> is the one HCT uses.</summary>
    public sealed class ViewingConditions
    {
        /// <summary>sRGB's D65 white, a mid-grey (L* 50) surround and an adapting luminance of about 11.7 cd/m².</summary>
        public static readonly ViewingConditions Default = Make(
            ColorUtils.WHITE_POINT_D65,
            200.0 / Math.PI * ColorUtils.YFromLstar(50.0) / 100.0,
            50.0,
            2.0,
            false);

        public readonly double N;
        public readonly double Aw;
        public readonly double Nbb;
        public readonly double Ncb;
        public readonly double C;
        public readonly double Nc;
        public readonly double[] RgbD;
        public readonly double Fl;
        public readonly double FlRoot;
        public readonly double Z;

        private ViewingConditions(double n, double aw, double nbb, double ncb, double c, double nc, double[] rgbD, double fl, double flRoot, double z)
        {
            N = n;
            Aw = aw;
            Nbb = nbb;
            Ncb = ncb;
            C = c;
            Nc = nc;
            RgbD = rgbD;
            Fl = fl;
            FlRoot = flRoot;
            Z = z;
        }

        public static ViewingConditions Make(double[] whitePoint, double adaptingLuminance, double backgroundLstar, double surround, bool discountingIlluminant)
        {
            backgroundLstar = Math.Max(0.1, backgroundLstar);

            var matrix = Cam16.XYZ_TO_CAM16RGB;
            var xyz = whitePoint;
            double rW = xyz[0] * matrix[0][0] + xyz[1] * matrix[0][1] + xyz[2] * matrix[0][2];
            double gW = xyz[0] * matrix[1][0] + xyz[1] * matrix[1][1] + xyz[2] * matrix[1][2];
            double bW = xyz[0] * matrix[2][0] + xyz[1] * matrix[2][1] + xyz[2] * matrix[2][2];

            double f = 0.8 + surround / 10.0;
            double c = f >= 0.9 ? MathUtils.Lerp(0.59, 0.69, (f - 0.9) * 10.0) : MathUtils.Lerp(0.525, 0.59, (f - 0.8) * 10.0);
            double d = discountingIlluminant ? 1.0 : f * (1.0 - 1.0 / 3.6 * Math.Exp((-adaptingLuminance - 42.0) / 92.0));
            d = Math.Clamp(d, 0.0, 1.0);

            double nc = f;
            double[] rgbD = { d * (100.0 / rW) + 1.0 - d, d * (100.0 / gW) + 1.0 - d, d * (100.0 / bW) + 1.0 - d };
            double k = 1.0 / (5.0 * adaptingLuminance + 1.0);
            double k4 = k * k * k * k;
            double k4F = 1.0 - k4;
            double fl = k4 * adaptingLuminance + 0.1 * k4F * k4F * Math.Cbrt(5.0 * adaptingLuminance);
            double n = ColorUtils.YFromLstar(backgroundLstar) / whitePoint[1];
            double z = 1.48 + Math.Sqrt(n);
            double nbb = 0.725 / Math.Pow(n, 0.2);
            double ncb = nbb;

            double[] rgbAFactors =
            {
                Math.Pow(fl * rgbD[0] * rW / 100.0, 0.42),
                Math.Pow(fl * rgbD[1] * gW / 100.0, 0.42),
                Math.Pow(fl * rgbD[2] * bW / 100.0, 0.42),
            };

            double[] rgbA =
            {
                400.0 * rgbAFactors[0] / (rgbAFactors[0] + 27.13),
                400.0 * rgbAFactors[1] / (rgbAFactors[1] + 27.13),
                400.0 * rgbAFactors[2] / (rgbAFactors[2] + 27.13),
            };

            double aw = (2.0 * rgbA[0] + rgbA[1] + 0.05 * rgbA[2]) * nbb;
            return new ViewingConditions(n, aw, nbb, ncb, c, nc, rgbD, fl, Math.Pow(fl, 0.25), z);
        }
    }

    /// <summary>
    /// CAM16, the colour appearance model behind HCT's hue and chroma, with its UCS coordinates for colour distances.
    /// Ported from material-color-utilities.
    /// </summary>
    public sealed class Cam16
    {
        internal static readonly double[][] XYZ_TO_CAM16RGB =
        {
            new[] { 0.401288, 0.650173, -0.051461 },
            new[] { -0.250268, 1.204414, 0.045854 },
            new[] { -0.002079, 0.048952, 0.953127 },
        };

        internal static readonly double[][] CAM16RGB_TO_XYZ =
        {
            new[] { 1.8620678, -1.0112547, 0.14918678 },
            new[] { 0.38752654, 0.62144744, -0.00897398 },
            new[] { -0.01584150, -0.03412294, 1.0499644 },
        };

        public double Hue { get; }
        public double Chroma { get; }
        public double J { get; }
        public double Q { get; }
        public double M { get; }
        public double S { get; }
        public double Jstar { get; }
        public double Astar { get; }
        public double Bstar { get; }

        private Cam16(double hue, double chroma, double j, double q, double m, double s, double jstar, double astar, double bstar)
        {
            Hue = hue;
            Chroma = chroma;
            J = j;
            Q = q;
            M = m;
            S = s;
            Jstar = jstar;
            Astar = astar;
            Bstar = bstar;
        }

        /// <summary>The perceptual distance to another colour, in CAM16-UCS (about 1 is just noticeable).</summary>
        public double Distance(Cam16 other)
        {
            double dJ = Jstar - other.Jstar;
            double dA = Astar - other.Astar;
            double dB = Bstar - other.Bstar;
            double dEPrime = Math.Sqrt(dJ * dJ + dA * dA + dB * dB);
            return 1.41 * Math.Pow(dEPrime, 0.63);
        }

        public static Cam16 FromInt(int argb) => FromIntInViewingConditions(argb, ViewingConditions.Default);

        public static Cam16 FromIntInViewingConditions(int argb, ViewingConditions vc)
        {
            var xyz = ColorUtils.XyzFromArgb(argb);
            return FromXyzInViewingConditions(xyz[0], xyz[1], xyz[2], vc);
        }

        public static Cam16 FromXyzInViewingConditions(double x, double y, double z, ViewingConditions vc)
        {
            // XYZ to cone responses, discounted for the illuminant.
            double rC = 0.401288 * x + 0.650173 * y - 0.051461 * z;
            double gC = -0.250268 * x + 1.204414 * y + 0.045854 * z;
            double bC = -0.002079 * x + 0.048952 * y + 0.953127 * z;

            double rD = vc.RgbD[0] * rC;
            double gD = vc.RgbD[1] * gC;
            double bD = vc.RgbD[2] * bC;

            // Chromatic adaptation.
            double rAF = Math.Pow(vc.Fl * Math.Abs(rD) / 100.0, 0.42);
            double gAF = Math.Pow(vc.Fl * Math.Abs(gD) / 100.0, 0.42);
            double bAF = Math.Pow(vc.Fl * Math.Abs(bD) / 100.0, 0.42);
            double rA = Math.Sign(rD) * 400.0 * rAF / (rAF + 27.13);
            double gA = Math.Sign(gD) * 400.0 * gAF / (gAF + 27.13);
            double bA = Math.Sign(bD) * 400.0 * bAF / (bAF + 27.13);

            // Redness-greenness and yellowness-blueness.
            double a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
            double b = (rA + gA - 2.0 * bA) / 9.0;

            double u = (20.0 * rA + 20.0 * gA + 21.0 * bA) / 20.0;
            double p2 = (40.0 * rA + 20.0 * gA + bA) / 20.0;

            double atanDegrees = MathUtils.ToDegrees(Math.Atan2(b, a));
            double hue = atanDegrees < 0 ? atanDegrees + 360.0 : atanDegrees >= 360 ? atanDegrees - 360.0 : atanDegrees;
            double hueRadians = MathUtils.ToRadians(hue);

            double ac = p2 * vc.Nbb;
            double j = 100.0 * Math.Pow(ac / vc.Aw, vc.C * vc.Z);
            double q = 4.0 / vc.C * Math.Sqrt(j / 100.0) * (vc.Aw + 4.0) * vc.FlRoot;

            double huePrime = hue < 20.14 ? hue + 360 : hue;
            double eHue = 0.25 * (Math.Cos(MathUtils.ToRadians(huePrime) + 2.0) + 3.8);
            double p1 = 50000.0 / 13.0 * eHue * vc.Nc * vc.Ncb;
            double t = p1 * Math.Sqrt(a * a + b * b) / (u + 0.305);
            double alpha = Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73) * Math.Pow(t, 0.9);

            double c = alpha * Math.Sqrt(j / 100.0);
            double m = c * vc.FlRoot;
            double s = 50.0 * Math.Sqrt(alpha * vc.C / (vc.Aw + 4.0));

            double jstar = (1.0 + 100.0 * 0.007) * j / (1.0 + 0.007 * j);
            double mstar = 1.0 / 0.0228 * Math.Log(1.0 + 0.0228 * m);
            double astar = mstar * Math.Cos(hueRadians);
            double bstar = mstar * Math.Sin(hueRadians);

            return new Cam16(hue, c, j, q, m, s, jstar, astar, bstar);
        }

        public static Cam16 FromJch(double j, double c, double h) => FromJchInViewingConditions(j, c, h, ViewingConditions.Default);

        public static Cam16 FromJchInViewingConditions(double j, double c, double h, ViewingConditions vc)
        {
            double q = 4.0 / vc.C * Math.Sqrt(j / 100.0) * (vc.Aw + 4.0) * vc.FlRoot;
            double m = c * vc.FlRoot;
            double alpha = c / Math.Sqrt(j / 100.0);
            double s = 50.0 * Math.Sqrt(alpha * vc.C / (vc.Aw + 4.0));

            double hueRadians = MathUtils.ToRadians(h);
            double jstar = (1.0 + 100.0 * 0.007) * j / (1.0 + 0.007 * j);
            double mstar = 1.0 / 0.0228 * Math.Log(1.0 + 0.0228 * m);
            double astar = mstar * Math.Cos(hueRadians);
            double bstar = mstar * Math.Sin(hueRadians);
            return new Cam16(h, c, j, q, m, s, jstar, astar, bstar);
        }

        /// <summary>The sRGB colour for this appearance under the default viewing conditions, clipped to the gamut.</summary>
        public int ToInt() => Viewed(ViewingConditions.Default);

        public int Viewed(ViewingConditions vc)
        {
            var xyz = XyzInViewingConditions(vc);
            return ColorUtils.ArgbFromXyz(xyz[0], xyz[1], xyz[2]);
        }

        public double[] XyzInViewingConditions(ViewingConditions vc)
        {
            double alpha = Chroma == 0.0 || J == 0.0 ? 0.0 : Chroma / Math.Sqrt(J / 100.0);

            double t = Math.Pow(alpha / Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73), 1.0 / 0.9);
            double hRad = MathUtils.ToRadians(Hue);

            double eHue = 0.25 * (Math.Cos(hRad + 2.0) + 3.8);
            double ac = vc.Aw * Math.Pow(J / 100.0, 1.0 / vc.C / vc.Z);
            double p1 = eHue * (50000.0 / 13.0) * vc.Nc * vc.Ncb;
            double p2 = ac / vc.Nbb;

            double hSin = Math.Sin(hRad);
            double hCos = Math.Cos(hRad);

            double gamma = 23.0 * (p2 + 0.305) * t / (23.0 * p1 + 11.0 * t * hCos + 108.0 * t * hSin);
            double a = gamma * hCos;
            double b = gamma * hSin;
            double rA = (460.0 * p2 + 451.0 * a + 288.0 * b) / 1403.0;
            double gA = (460.0 * p2 - 891.0 * a - 261.0 * b) / 1403.0;
            double bA = (460.0 * p2 - 220.0 * a - 6300.0 * b) / 1403.0;

            double rCBase = Math.Max(0, 27.13 * Math.Abs(rA) / (400.0 - Math.Abs(rA)));
            double rC = Math.Sign(rA) * (100.0 / vc.Fl) * Math.Pow(rCBase, 1.0 / 0.42);
            double gCBase = Math.Max(0, 27.13 * Math.Abs(gA) / (400.0 - Math.Abs(gA)));
            double gC = Math.Sign(gA) * (100.0 / vc.Fl) * Math.Pow(gCBase, 1.0 / 0.42);
            double bCBase = Math.Max(0, 27.13 * Math.Abs(bA) / (400.0 - Math.Abs(bA)));
            double bC = Math.Sign(bA) * (100.0 / vc.Fl) * Math.Pow(bCBase, 1.0 / 0.42);

            double rF = rC / vc.RgbD[0];
            double gF = gC / vc.RgbD[1];
            double bF = bC / vc.RgbD[2];

            var matrix = CAM16RGB_TO_XYZ;
            return new[]
            {
                rF * matrix[0][0] + gF * matrix[0][1] + bF * matrix[0][2],
                rF * matrix[1][0] + gF * matrix[1][1] + bF * matrix[1][2],
                rF * matrix[2][0] + gF * matrix[2][1] + bF * matrix[2][2],
            };
        }
    }
}
