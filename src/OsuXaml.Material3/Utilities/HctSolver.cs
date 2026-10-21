using System;

namespace OsuXaml.Material3.Utilities
{
    /// <summary>
    /// Finds the sRGB colour for a hue, chroma and tone: exactly when sRGB has it, else the colour of that hue and tone on the
    /// edge of the gamut, which has the most chroma there is. Ported from material-color-utilities' HctSolver, so palettes
    /// match Material Theme Builder's and Android's to the bit.
    /// </summary>
    internal static class HctSolver
    {
        // Linear RGB (0 to 100) to CAM16 RGB, discounted for the default viewing conditions' white and scaled by FL / 100:
        // what chromatic adaptation takes. The reference has these as constants; deriving them gives the same values.
        private static readonly double[][] scaled_discount_from_linrgb;
        private static readonly double[][] linrgb_from_scaled_discount;
        private static readonly double[] y_from_linrgb = ColorUtils.SRGB_TO_XYZ[1];

        // The linear RGB values half way between neighbouring 8-bit sRGB values.
        private static readonly double[] critical_planes = new double[255];

        static HctSolver()
        {
            var vc = ViewingConditions.Default;
            var matrix = new double[3][];

            for (int i = 0; i < 3; i++)
            {
                matrix[i] = new double[3];

                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += Cam16.XYZ_TO_CAM16RGB[i][k] * ColorUtils.SRGB_TO_XYZ[k][j];

                    matrix[i][j] = sum * vc.RgbD[i] * vc.Fl / 100.0;
                }
            }

            scaled_discount_from_linrgb = matrix;
            linrgb_from_scaled_discount = invert(matrix);

            for (int i = 0; i < critical_planes.Length; i++)
            {
                double normalized = (i + 0.5) / 255.0;
                critical_planes[i] = (normalized <= 0.040449936 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4)) * 100.0;
            }
        }

        public static int SolveToInt(double hueDegrees, double chroma, double lstar)
        {
            if (chroma < 0.0001 || lstar < 0.0001 || lstar > 99.9999)
                return ColorUtils.ArgbFromLstar(lstar);

            double hueRadians = MathUtils.SanitizeDegrees(hueDegrees) / 180.0 * Math.PI;
            double y = ColorUtils.YFromLstar(lstar);

            int exactAnswer = findResultByJ(hueRadians, chroma, y);
            if (exactAnswer != 0)
                return exactAnswer;

            return ColorUtils.ArgbFromLinrgb(bisectToLimit(y, hueRadians));
        }

        private static double sanitizeRadians(double angle) => (angle + Math.PI * 8) % (Math.PI * 2);

        /// <summary>A linear RGB component (0 to 100) as sRGB (0 to 255), unrounded.</summary>
        private static double trueDelinearized(double rgbComponent)
        {
            double normalized = rgbComponent / 100.0;
            double delinearized = normalized <= 0.0031308 ? normalized * 12.92 : 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;
            return delinearized * 255.0;
        }

        private static double chromaticAdaptation(double component)
        {
            double af = Math.Pow(Math.Abs(component), 0.42);
            return Math.Sign(component) * 400.0 * af / (af + 27.13);
        }

        private static double inverseChromaticAdaptation(double adapted)
        {
            double adaptedAbs = Math.Abs(adapted);
            double b = Math.Max(0, 27.13 * adaptedAbs / (400.0 - adaptedAbs));
            return Math.Sign(adapted) * Math.Pow(b, 1.0 / 0.42);
        }

        /// <summary>The CAM16 hue of a linear RGB colour, in radians.</summary>
        private static double hueOf(double[] linrgb)
        {
            var scaledDiscount = multiply(linrgb, scaled_discount_from_linrgb);
            double rA = chromaticAdaptation(scaledDiscount[0]);
            double gA = chromaticAdaptation(scaledDiscount[1]);
            double bA = chromaticAdaptation(scaledDiscount[2]);

            double a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
            double b = (rA + gA - 2.0 * bA) / 9.0;
            return Math.Atan2(b, a);
        }

        private static bool areInCyclicOrder(double a, double b, double c) => sanitizeRadians(b - a) < sanitizeRadians(c - a);

        private static double[] lerpPoint(double[] source, double t, double[] target) =>
            new[]
            {
                source[0] + (target[0] - source[0]) * t,
                source[1] + (target[1] - source[1]) * t,
                source[2] + (target[2] - source[2]) * t,
            };

        private static double[] setCoordinate(double[] source, double coordinate, double[] target, int axis)
            => lerpPoint(source, (coordinate - source[axis]) / (target[axis] - source[axis]), target);

        private static bool isBounded(double x) => 0.0 <= x && x <= 100.0;

        private static readonly double[] none = { -1.0, -1.0, -1.0 };

        /// <summary>
        /// The nth of the 12 edges of the RGB cube crossed with the plane of luminance <paramref name="y"/>, or a negative
        /// point when the plane misses that edge.
        /// </summary>
        private static double[] nthVertex(double y, int n)
        {
            double kR = y_from_linrgb[0];
            double kG = y_from_linrgb[1];
            double kB = y_from_linrgb[2];
            double coordA = n % 4 <= 1 ? 0.0 : 100.0;
            double coordB = n % 2 == 0 ? 0.0 : 100.0;

            if (n < 4)
            {
                double r = (y - coordA * kG - coordB * kB) / kR;
                return isBounded(r) ? new[] { r, coordA, coordB } : none;
            }

            if (n < 8)
            {
                double g = (y - coordB * kR - coordA * kB) / kG;
                return isBounded(g) ? new[] { coordB, g, coordA } : none;
            }

            double bl = (y - coordA * kR - coordB * kG) / kB;
            return isBounded(bl) ? new[] { coordA, coordB, bl } : none;
        }

        /// <summary>The two vertices of the gamut's slice at <paramref name="y"/> whose hues bracket the target hue.</summary>
        private static (double[] Left, double[] Right) bisectToSegment(double y, double targetHue)
        {
            double[] left = none;
            double[] right = none;
            double leftHue = 0.0;
            double rightHue = 0.0;
            bool initialized = false;
            bool uncut = true;

            for (int n = 0; n < 12; n++)
            {
                var mid = nthVertex(y, n);

                if (mid[0] < 0)
                    continue;

                double midHue = hueOf(mid);

                if (!initialized)
                {
                    left = mid;
                    right = mid;
                    leftHue = midHue;
                    rightHue = midHue;
                    initialized = true;
                    continue;
                }

                if (uncut || areInCyclicOrder(leftHue, midHue, rightHue))
                {
                    uncut = false;

                    if (areInCyclicOrder(leftHue, targetHue, midHue))
                    {
                        right = mid;
                        rightHue = midHue;
                    }
                    else
                    {
                        left = mid;
                        leftHue = midHue;
                    }
                }
            }

            return (left, right);
        }

        private static double[] midpoint(double[] a, double[] b) => new[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 };

        private static int criticalPlaneBelow(double x) => (int)Math.Floor(x - 0.5);

        private static int criticalPlaneAbove(double x) => (int)Math.Ceiling(x - 0.5);

        /// <summary>The point of the target hue on the gamut's edge at luminance <paramref name="y"/>, found to 8-bit precision.</summary>
        private static double[] bisectToLimit(double y, double targetHue)
        {
            var (left, right) = bisectToSegment(y, targetHue);
            double leftHue = hueOf(left);

            for (int axis = 0; axis < 3; axis++)
            {
                if (left[axis] == right[axis])
                    continue;

                int lPlane;
                int rPlane;

                if (left[axis] < right[axis])
                {
                    lPlane = criticalPlaneBelow(trueDelinearized(left[axis]));
                    rPlane = criticalPlaneAbove(trueDelinearized(right[axis]));
                }
                else
                {
                    lPlane = criticalPlaneAbove(trueDelinearized(left[axis]));
                    rPlane = criticalPlaneBelow(trueDelinearized(right[axis]));
                }

                for (int i = 0; i < 8; i++)
                {
                    if (Math.Abs(rPlane - lPlane) <= 1)
                        break;

                    int mPlane = (int)Math.Floor((lPlane + rPlane) / 2.0);
                    var mid = setCoordinate(left, critical_planes[mPlane], right, axis);
                    double midHue = hueOf(mid);

                    if (areInCyclicOrder(leftHue, targetHue, midHue))
                    {
                        right = mid;
                        rPlane = mPlane;
                    }
                    else
                    {
                        left = mid;
                        leftHue = midHue;
                        lPlane = mPlane;
                    }
                }
            }

            return midpoint(left, right);
        }

        /// <summary>
        /// Solves CAM16 for the lightness J that gives luminance <paramref name="y"/> at this hue and chroma, by Newton's method;
        /// 0 when the colour is outside sRGB.
        /// </summary>
        private static int findResultByJ(double hueRadians, double chroma, double y)
        {
            double j = Math.Sqrt(y) * 11.0;

            var vc = ViewingConditions.Default;
            double tInnerCoeff = 1 / Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73);
            double eHue = 0.25 * (Math.Cos(hueRadians + 2.0) + 3.8);
            double p1 = eHue * (50000.0 / 13.0) * vc.Nc * vc.Ncb;
            double hSin = Math.Sin(hueRadians);
            double hCos = Math.Cos(hueRadians);

            for (int iterationRound = 0; iterationRound < 5; iterationRound++)
            {
                double jNormalized = j / 100.0;
                double alpha = chroma == 0.0 || j == 0.0 ? 0.0 : chroma / Math.Sqrt(jNormalized);
                double t = Math.Pow(alpha * tInnerCoeff, 1.0 / 0.9);
                double ac = vc.Aw * Math.Pow(jNormalized, 1.0 / vc.C / vc.Z);
                double p2 = ac / vc.Nbb;
                double gamma = 23.0 * (p2 + 0.305) * t / (23.0 * p1 + 11 * t * hCos + 108.0 * t * hSin);
                double a = gamma * hCos;
                double b = gamma * hSin;
                double rA = (460.0 * p2 + 451.0 * a + 288.0 * b) / 1403.0;
                double gA = (460.0 * p2 - 891.0 * a - 261.0 * b) / 1403.0;
                double bA = (460.0 * p2 - 220.0 * a - 6300.0 * b) / 1403.0;

                var linrgb = multiply(new[] { inverseChromaticAdaptation(rA), inverseChromaticAdaptation(gA), inverseChromaticAdaptation(bA) },
                    linrgb_from_scaled_discount);

                if (linrgb[0] < 0 || linrgb[1] < 0 || linrgb[2] < 0)
                    return 0;

                double fnj = y_from_linrgb[0] * linrgb[0] + y_from_linrgb[1] * linrgb[1] + y_from_linrgb[2] * linrgb[2];

                if (fnj <= 0)
                    return 0;

                if (iterationRound == 4 || Math.Abs(fnj - y) < 0.002)
                {
                    if (linrgb[0] > 100.01 || linrgb[1] > 100.01 || linrgb[2] > 100.01)
                        return 0;

                    return ColorUtils.ArgbFromLinrgb(linrgb);
                }

                // Newton's method, with 2 * fn(j) / j standing in for fn'(j).
                j -= (fnj - y) * j / (2 * fnj);
            }

            return 0;
        }

        private static double[] multiply(double[] row, double[][] matrix) =>
            new[]
            {
                row[0] * matrix[0][0] + row[1] * matrix[0][1] + row[2] * matrix[0][2],
                row[0] * matrix[1][0] + row[1] * matrix[1][1] + row[2] * matrix[1][2],
                row[0] * matrix[2][0] + row[1] * matrix[2][1] + row[2] * matrix[2][2],
            };

        private static double[][] invert(double[][] m)
        {
            double det = m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1])
                         - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
                         + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]);

            return new[]
            {
                new[]
                {
                    (m[1][1] * m[2][2] - m[1][2] * m[2][1]) / det,
                    (m[0][2] * m[2][1] - m[0][1] * m[2][2]) / det,
                    (m[0][1] * m[1][2] - m[0][2] * m[1][1]) / det,
                },
                new[]
                {
                    (m[1][2] * m[2][0] - m[1][0] * m[2][2]) / det,
                    (m[0][0] * m[2][2] - m[0][2] * m[2][0]) / det,
                    (m[0][2] * m[1][0] - m[0][0] * m[1][2]) / det,
                },
                new[]
                {
                    (m[1][0] * m[2][1] - m[1][1] * m[2][0]) / det,
                    (m[0][1] * m[2][0] - m[0][0] * m[2][1]) / det,
                    (m[0][0] * m[1][1] - m[0][1] * m[1][0]) / det,
                },
            };
        }
    }
}
