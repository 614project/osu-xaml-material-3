using System;
using osu.Framework.Graphics.Transforms;

namespace OsuXaml.Material3
{
    /// <summary>A CSS-style cubic Bézier timing curve through (0,0), (x1,y1), (x2,y2) and (1,1).</summary>
    public readonly struct CubicBezier : IEasingFunction
    {
        private readonly double x1, y1, x2, y2;

        public CubicBezier(double x1, double y1, double x2, double y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public double ApplyEasing(double time)
        {
            if (time <= 0) return 0;
            if (time >= 1) return 1;

            // Solve x(t) = time for t with Newton's method, falling back to bisection, then evaluate y(t).
            double t = time;

            for (int i = 0; i < 8; i++)
            {
                double error = sample(x1, x2, t) - time;
                if (Math.Abs(error) < 1e-6)
                    return sample(y1, y2, t);

                double slope = slopeAt(x1, x2, t);
                if (Math.Abs(slope) < 1e-6)
                    break;

                t -= error / slope;
            }

            double low = 0, high = 1;
            t = time;

            for (int i = 0; i < 30; i++)
            {
                double x = sample(x1, x2, t);
                if (Math.Abs(x - time) < 1e-6)
                    break;

                if (x < time)
                    low = t;
                else
                    high = t;

                t = (low + high) / 2;
            }

            return sample(y1, y2, t);
        }

        private static double sample(double a, double b, double t) => ((1 - 3 * b + 3 * a) * t + (3 * b - 6 * a)) * t * t + 3 * a * t;

        private static double slopeAt(double a, double b, double t) => 3 * (1 - 3 * b + 3 * a) * t * t + 2 * (3 * b - 6 * a) * t + 3 * a;
    }

    /// <summary>
    /// Material 3's motion tokens: easing curves and durations (md.sys.motion). Emphasized curves suit transitions that
    /// draw attention (a sheet sliding in, a FAB expanding); standard ones suit small utility changes (a colour, a state layer).
    /// </summary>
    public static class Motion
    {
        /// <summary>Material's emphasized curve, approximated with one Bézier (the spec draws it with two).</summary>
        public static readonly CubicBezier Emphasized = new CubicBezier(0.2, 0, 0, 1);

        public static readonly CubicBezier EmphasizedDecelerate = new CubicBezier(0.05, 0.7, 0.1, 1);
        public static readonly CubicBezier EmphasizedAccelerate = new CubicBezier(0.3, 0, 0.8, 0.15);
        public static readonly CubicBezier Standard = new CubicBezier(0.2, 0, 0, 1);
        public static readonly CubicBezier StandardDecelerate = new CubicBezier(0, 0, 0, 1);
        public static readonly CubicBezier StandardAccelerate = new CubicBezier(0.3, 0, 1, 1);
        public static readonly CubicBezier Linear = new CubicBezier(0, 0, 1, 1);

        public const double Short1 = 50;
        public const double Short2 = 100;
        public const double Short3 = 150;
        public const double Short4 = 200;
        public const double Medium1 = 250;
        public const double Medium2 = 300;
        public const double Medium3 = 350;
        public const double Medium4 = 400;
        public const double Long1 = 450;
        public const double Long2 = 500;
        public const double Long3 = 550;
        public const double Long4 = 600;
        public const double ExtraLong1 = 700;
        public const double ExtraLong2 = 800;
        public const double ExtraLong3 = 900;
        public const double ExtraLong4 = 1000;
    }
}
