using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What both progress indicators share: <see cref="Value"/> from 0 to 1, or <see cref="IsIndeterminate"/> for work of unknown
    /// length. They don't take input.
    /// </summary>
    public abstract partial class ProgressIndicator : MaterialControl
    {
        private bool isIndeterminate;

        protected ProgressIndicator()
        {
            Value.BindValueChanged(e =>
            {
                if (IsLoaded)
                    this.TransformTo(nameof(ShownValue), (float)Math.Clamp(e.NewValue, 0, 1), Motion.Medium2, Motion.Standard);
                else
                    ShownValue = (float)Math.Clamp(e.NewValue, 0, 1);
            });
        }

        /// <summary>How much is done, from 0 to 1. Binds.</summary>
        public BindableDouble Value { get; } = new BindableDouble();

        /// <summary>Animates continuously instead of showing <see cref="Value"/>.</summary>
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                isIndeterminate = value;
                IndeterminateStart = double.NaN;
            }
        }

        /// <summary>The value as drawn, gliding after <see cref="Value"/>.</summary>
        protected float ShownValue { get; set; }

        /// <summary>When the indeterminate animation started.</summary>
        protected double IndeterminateStart { get; private set; } = double.NaN;

        protected override void Update()
        {
            base.Update();

            if (double.IsNaN(IndeterminateStart))
                IndeterminateStart = Time.Current;
        }

        protected override bool Interactive => false;

        public override bool HandlePositionalInput => false;
    }

    /// <summary>
    /// Material's linear progress indicator (2024): a 4dp rounded bar in Primary, a gap, the rest of the track in SecondaryContainer
    /// and a stop dot at the end. <c>&lt;m3:LinearProgressIndicator Value="{Bind Progress}" /&gt;</c>, or <c>IsIndeterminate="True"</c>.
    /// </summary>
    public partial class LinearProgressIndicator : ProgressIndicator
    {
        private const float height = 4;
        private const float gap = 4;

        // Material's indeterminate timing: two bars, each with a head and a tail, over 1.8 seconds.
        private const double cycle = 1800;
        private static readonly CubicBezier first_head_easing = new CubicBezier(0.2, 0, 0.8, 1);
        private static readonly CubicBezier first_tail_easing = new CubicBezier(0.4, 0, 1, 1);
        private static readonly CubicBezier second_head_easing = new CubicBezier(0, 0, 0.65, 1);
        private static readonly CubicBezier second_tail_easing = new CubicBezier(0.1, 0, 0.45, 1);

        private readonly Circle bar1;
        private readonly Circle bar2;
        private readonly Circle track1;
        private readonly Circle track2;
        private readonly Circle track3;
        private readonly Circle stopDot;

        public LinearProgressIndicator()
        {
            Height = height;

            InternalChildren = new Drawable[]
            {
                track1 = new Circle { RelativeSizeAxes = Axes.Y },
                track2 = new Circle { RelativeSizeAxes = Axes.Y },
                track3 = new Circle { RelativeSizeAxes = Axes.Y },
                stopDot = new Circle { Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, Size = new Vector2(height) },
                bar1 = new Circle { RelativeSizeAxes = Axes.Y },
                bar2 = new Circle { RelativeSizeAxes = Axes.Y },
            };
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(240, height);

        protected override void UpdateColors(bool animate)
        {
            foreach (var bar in new[] { bar1, bar2, stopDot })
                SetColour(bar, Scheme.Primary, animate);

            foreach (var track in new[] { track1, track2, track3 })
                SetColour(track, Scheme.SecondaryContainer, animate);
        }

        protected override void Update()
        {
            base.Update();

            float w = DrawWidth;

            if (!IsIndeterminate)
            {
                float end = w * ShownValue;
                place(bar1, 0, end);
                hide(bar2);

                place(track1, end + gap, w);
                hide(track2);
                hide(track3);
                stopDot.Alpha = w - end > gap + height ? 1 : 0;
                return;
            }

            stopDot.Alpha = 0;
            double t = (Time.Current - IndeterminateStart) % cycle;

            float head1 = progress(t, 0, 750, first_head_easing);
            float tail1 = progress(t, 333, 850, first_tail_easing);
            float head2 = progress(t, 1000, 567, second_head_easing);
            float tail2 = progress(t, 1267, 533, second_tail_easing);

            float b1s = w * tail1, b1e = w * head1;
            float b2s = w * tail2, b2e = w * head2;

            place(bar1, b1s, b1e);
            place(bar2, b2s, b2e);

            // The track fills what the bars leave, with a gap beside each bar.
            bool b1 = b1e - b1s > 0.5f, b2 = b2e - b2s > 0.5f;

            if (b2)
            {
                place(track1, 0, b2s - gap);
                place(track2, b2e + gap, b1 ? b1s - gap : w);
                if (b1) place(track3, b1e + gap, w);
                else hide(track3);
            }
            else if (b1)
            {
                place(track1, 0, b1s - gap);
                place(track2, b1e + gap, w);
                hide(track3);
            }
            else
            {
                place(track1, 0, w);
                hide(track2);
                hide(track3);
            }
        }

        private static float progress(double t, double delay, double duration, CubicBezier easing)
            => (float)easing.ApplyEasing(Math.Clamp((t - delay) / duration, 0, 1));

        private static void place(Drawable d, float start, float end)
        {
            if (end - start < 0.5f)
            {
                hide(d);
                return;
            }

            d.Alpha = 1;
            d.X = start;
            d.Width = end - start;
        }

        private static void hide(Drawable d) => d.Alpha = 0;
    }

    /// <summary>
    /// Material's circular progress indicator (2024): a 4dp arc in Primary over a SecondaryContainer track, with gaps between them,
    /// 40dp across. <c>&lt;m3:CircularProgressIndicator IsIndeterminate="True" /&gt;</c>.
    /// </summary>
    public partial class CircularProgressIndicator : ProgressIndicator
    {
        private const float stroke = 4;
        private const float gap = 4;

        // Material's indeterminate timing: the arc grows and shrinks once per 1332ms while the whole spins.
        private const double cycle = 1332;
        private const float jump = 290;
        private const float base_rotation = 286;
        private static readonly CubicBezier arc_easing = new CubicBezier(0.4, 0, 0.2, 1);

        private readonly CircularProgress active;
        private readonly CircularProgress track;
        private float size = 40;

        public CircularProgressIndicator()
        {
            InternalChildren = new Drawable[]
            {
                track = new CircularProgress { RelativeSizeAxes = Axes.Both, Anchor = Anchor.Centre, Origin = Anchor.Centre, RoundedCaps = true },
                active = new CircularProgress { RelativeSizeAxes = Axes.Both, Anchor = Anchor.Centre, Origin = Anchor.Centre, RoundedCaps = true },
            };

            Size = new Vector2(size);
        }

        /// <summary>The diameter; 40 by default.</summary>
        public float Diameter
        {
            get => size;
            set
            {
                size = value;
                Size = new Vector2(value);
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(size);

        protected override void UpdateColors(bool animate)
        {
            SetColour(active, Scheme.Primary, animate);
            SetColour(track, Scheme.SecondaryContainer, animate);
        }

        protected override void Update()
        {
            base.Update();

            float radius = DrawWidth / 2;
            if (radius <= stroke)
                return;

            active.InnerRadius = track.InnerRadius = stroke / radius;

            // Rounded caps reach half the stroke past each end, so the arcs are shortened by that and the gap kept between them.
            float circumference = MathF.PI * 2 * (radius - stroke / 2);
            float capTurns = stroke / 2 / circumference;
            float gapTurns = gap / circumference;

            if (!IsIndeterminate)
            {
                float v = ShownValue;
                float activeTurns = Math.Max(0, v - 2 * capTurns);
                float trackTurns = Math.Max(0, 1 - v - 2 * gapTurns - 2 * capTurns);

                active.Alpha = v > 0.001f ? 1 : 0;
                active.Rotation = capTurns * 360;
                active.Progress = activeTurns;

                track.Alpha = trackTurns > 0 ? 1 : 0;
                track.Rotation = (v + gapTurns + capTurns) * 360;
                track.Progress = trackTurns;
                return;
            }

            track.Alpha = 0;
            active.Alpha = 1;

            double elapsed = Time.Current - IndeterminateStart;
            double t = elapsed % cycle;
            int turn = (int)(elapsed / cycle) % 5;

            float head = (float)arc_easing.ApplyEasing(Math.Clamp(t / (cycle / 2), 0, 1)) * jump;
            float tail = (float)arc_easing.ApplyEasing(Math.Clamp((t - cycle / 2) / (cycle / 2), 0, 1)) * jump;
            float rotation = (float)(t / cycle) * base_rotation + turn * ((base_rotation + jump) % 360);

            active.Rotation = rotation + tail;
            active.Progress = Math.Max(0.01f, (head - tail) / 360);
        }
    }
}
