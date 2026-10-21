using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's slider, in its current (2024) form: a 16dp track split by a 4dp handle with a gap either side, and a stop dot at
    /// the end. <c>&lt;m3:Slider Value="{Bind Volume}" Maximum="100" /&gt;</c>. With <see cref="StepSize"/> it snaps to steps and
    /// shows them as dots (<see cref="ShowTicks"/>); while dragging, a label above the handle shows the value.
    /// </summary>
    public partial class Slider : MaterialControl
    {
        private const float track_height = 16;
        private const float handle_width = 4;
        private const float handle_height = 44;
        private const float handle_gap = 6;
        private const float dot_size = 4;

        private readonly TrackSegment activeTrack;
        private readonly TrackSegment inactiveTrack;
        private readonly Container ticks;
        private readonly Circle stopDot;
        private readonly Container handle;
        private readonly Container valueLabel;
        private readonly Circle valueLabelFill;
        private readonly Text valueLabelText;

        private double minimum;
        private double maximum = 100;
        private double stepSize;
        private bool showTicks = true;
        private bool dragging;

        // Where the handle is drawn, as a fraction of the track; follows the value with a short glide.
        private float shownFraction;

        public Slider()
        {
            Height = handle_height;

            InternalChildren = new Drawable[]
            {
                activeTrack = new TrackSegment(roundStart: true) { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Height = track_height },
                inactiveTrack = new TrackSegment(roundStart: false) { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Height = track_height },
                ticks = new Container { RelativeSizeAxes = Axes.Both },
                stopDot = new Circle { Anchor = Anchor.CentreRight, Origin = Anchor.Centre, Size = new Vector2(dot_size), X = -track_height / 2 },
                handle = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                    Size = new Vector2(handle_width, handle_height),
                    Masking = true,
                    CornerRadius = handle_width / 2,
                    Child = new Box { RelativeSizeAxes = Axes.Both },
                },
                valueLabel = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.BottomCentre,
                    Size = new Vector2(48, 44),
                    Y = -handle_height / 2 - 4,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        valueLabelFill = new Circle { RelativeSizeAxes = Axes.Both },
                        valueLabelText = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre },
                    },
                },
            };

            Value.BindValueChanged(e =>
            {
                double clamped = clamp(e.NewValue);

                if (clamped != e.NewValue)
                {
                    Value.Value = clamped;
                    return;
                }

                valueLabelText.Text = FormatValue(clamped);

                if (IsLoaded)
                    this.TransformTo(nameof(shownFraction), fraction(clamped), dragging ? 0 : Motion.Short4, Motion.Standard);
                else
                    shownFraction = fraction(clamped);
            });
        }

        /// <summary>The value, between <see cref="Minimum"/> and <see cref="Maximum"/>. Binds two-way.</summary>
        public BindableDouble Value { get; } = new BindableDouble();

        public double Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                rangeChanged();
            }
        }

        public double Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                rangeChanged();
            }
        }

        /// <summary>The step the value snaps to; 0 (the default) for a continuous slider.</summary>
        public double StepSize
        {
            get => stepSize;
            set
            {
                stepSize = Math.Max(0, value);
                rangeChanged();
            }
        }

        /// <summary>Whether a stepped slider shows its steps as dots (true by default).</summary>
        public bool ShowTicks
        {
            get => showTicks;
            set
            {
                showTicks = value;
                rangeChanged();
            }
        }

        /// <summary>Whether a label above the handle shows the value while it is dragged (true by default).</summary>
        public bool ShowValueLabel { get; set; } = true;

        /// <summary>How the value label writes the value; "0" (a whole number) by default.</summary>
        public string ValueFormat { get; set; } = "0";

        /// <summary>The text of the value label.</summary>
        protected virtual string FormatValue(double value) => value.ToString(ValueFormat, CultureInfo.CurrentCulture);

        public override bool AcceptsFocus => IsEnabled;

        private void rangeChanged()
        {
            Value.Value = clamp(Value.Value);
            shownFraction = fraction(Value.Value);
            buildTicks();
        }

        private double clamp(double value)
        {
            double low = Math.Min(minimum, maximum), high = Math.Max(minimum, maximum);
            value = Math.Clamp(value, low, high);

            if (stepSize > 0)
                value = Math.Clamp(minimum + Math.Round((value - minimum) / stepSize) * stepSize, low, high);

            return value;
        }

        private float fraction(double value) => maximum == minimum ? 0 : (float)Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);

        private int stepCount => stepSize > 0 && maximum > minimum ? (int)Math.Min(1000, Math.Floor((maximum - minimum) / stepSize)) : 0;

        private readonly List<Circle> tickDots = new List<Circle>();

        private void buildTicks()
        {
            ticks.Clear();
            tickDots.Clear();

            if (!showTicks || stepCount < 1 || stepCount > 100)
                return;

            for (int i = 0; i <= stepCount; i++)
            {
                var dot = new Circle { Anchor = Anchor.CentreLeft, Origin = Anchor.Centre, Size = new Vector2(dot_size) };
                ticks.Add(dot);
                tickDots.Add(dot);
            }

            if (IsLoaded)
                UpdateColors(false);
        }

        // The handle's centre travels between these, so the track keeps its rounded ends at either extreme.
        private float travelStart => handle_width / 2;

        private float travelEnd => Math.Max(travelStart, DrawWidth - handle_width / 2);

        private float xAt(float f) => travelStart + (travelEnd - travelStart) * f;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float x = xAt(shownFraction);
            handle.X = x;
            valueLabel.X = x;

            float activeEnd = x - handle_width / 2 - handle_gap;
            float inactiveStart = x + handle_width / 2 + handle_gap;

            activeTrack.Alpha = activeEnd > 0 ? 1 : 0;
            activeTrack.Width = Math.Max(0, activeEnd);

            inactiveTrack.Alpha = inactiveStart < DrawWidth ? 1 : 0;
            inactiveTrack.X = inactiveStart;
            inactiveTrack.Width = Math.Max(0, DrawWidth - inactiveStart);

            // Ticks closer than twice their size would run into a dotted line, so the slider shows none then.
            bool showDots = tickDots.Count > 0 && (travelEnd - travelStart) / stepCount >= dot_size * 2;

            // Dots under or right beside the handle are hidden; the stop dot gives way to a tick at the end.
            stopDot.Alpha = !showDots && DrawWidth - track_height / 2 > inactiveStart + dot_size ? 1 : 0;

            for (int i = 0; i < tickDots.Count; i++)
            {
                float t = (float)i / stepCount;
                float tx = Math.Clamp(xAt(t), track_height / 2, Math.Max(track_height / 2, DrawWidth - track_height / 2));
                tickDots[i].X = tx;
                tickDots[i].Alpha = showDots && (tx < activeEnd - dot_size || tx > inactiveStart + dot_size) ? 1 : 0;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(200, handle_height);

        protected override void UpdateColors(bool animate)
        {
            Colour4 active = IsEnabled ? Scheme.Primary : Scheme.OnSurface.Opacity(StateOpacity.DisabledContent);
            Colour4 inactive = IsEnabled ? Scheme.SecondaryContainer : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);

            SetColour(activeTrack, active, animate);
            SetColour(inactiveTrack, inactive, animate);
            SetColour(handle, active, animate);
            SetColour(stopDot, IsEnabled ? Scheme.Primary : Scheme.OnSurface.Opacity(StateOpacity.DisabledContent), animate);

            float f = fraction(Value.Value);

            for (int i = 0; i < tickDots.Count; i++)
            {
                bool onActive = (float)i / stepCount < f;
                SetColour(tickDots[i], !IsEnabled ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContent)
                    : onActive ? Scheme.OnPrimary : Scheme.OnSecondaryContainer, animate);
            }

            SetColour(valueLabelFill, Scheme.InverseSurface, animate);
            SetColour(valueLabelText, Scheme.InverseOnSurface, animate);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Value.BindValueChanged(_ => UpdateColors(true));
        }

        #region Input

        protected override void OnPressed(Vector2 screenSpacePosition)
        {
            // Focus on press, for the arrow keys: a drag ends without the click that would otherwise focus the slider.
            GetContainingFocusManager()?.ChangeFocus(this);

            dragging = true;
            setFromScreen(screenSpacePosition);
            showDragState(true);
        }

        protected override void OnReleased()
        {
            dragging = false;
            showDragState(false);
        }

        // Handled, so the click doesn't move the focus away again.
        protected override bool OnClick(ClickEvent e) => IsEnabled;

        protected override bool OnDragStart(DragStartEvent e) => IsEnabled && e.Button == MouseButton.Left;

        protected override void OnDrag(DragEvent e)
        {
            if (IsEnabled)
                setFromScreen(e.ScreenSpaceMousePosition);
        }

        private void showDragState(bool active)
        {
            handle.ResizeWidthTo(active ? 2 : handle_width, Motion.Short3, Motion.Standard);

            if (ShowValueLabel)
                valueLabel.FadeTo(active ? 1 : 0, Motion.Short3, Motion.Standard).ScaleTo(active ? 1 : 0.6f, Motion.Short4, Motion.EmphasizedDecelerate);
        }

        private void setFromScreen(Vector2 screenSpacePosition)
        {
            if (Value.Disabled)
                return;

            float x = ToLocalSpace(screenSpacePosition).X;
            float f = travelEnd > travelStart ? Math.Clamp((x - travelStart) / (travelEnd - travelStart), 0, 1) : 0;
            Value.Value = clamp(minimum + (maximum - minimum) * f);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!HasFocus || !IsEnabled || Value.Disabled)
                return base.OnKeyDown(e);

            double step = stepSize > 0 ? stepSize : (maximum - minimum) / 100;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Down:
                    Value.Value = clamp(Value.Value - step);
                    return true;

                case Key.Right:
                case Key.Up:
                    Value.Value = clamp(Value.Value + step);
                    return true;

                case Key.Home:
                    Value.Value = minimum;
                    return true;

                case Key.End:
                    Value.Value = maximum;
                    return true;
            }

            return base.OnKeyDown(e);
        }

        #endregion

        /// <summary>
        /// A piece of track whose outer end is fully rounded and whose end by the handle has Material's small 2dp corners.
        /// The two parts meet without overlapping, so a translucent (disabled) colour stays even.
        /// </summary>
        private partial class TrackSegment : CompositeDrawable
        {
            private const float cap = track_height / 2;
            private const float inner_radius = 2;

            private readonly Container capClip;
            private readonly Container bodyClip;
            private readonly Container body;

            public TrackSegment(bool roundStart)
            {
                var near = roundStart ? Anchor.CentreLeft : Anchor.CentreRight;
                var far = roundStart ? Anchor.CentreRight : Anchor.CentreLeft;

                InternalChildren = new Drawable[]
                {
                    capClip = new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Anchor = near,
                        Origin = near,
                        Masking = true,
                        Child = new Circle { Size = new Vector2(track_height), Anchor = near, Origin = near },
                    },
                    bodyClip = new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Anchor = far,
                        Origin = far,
                        Masking = true,
                        Child = body = new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            Anchor = far,
                            Origin = far,
                            Masking = true,
                            CornerRadius = inner_radius,
                            Child = new Box { RelativeSizeAxes = Axes.Both },
                        },
                    },
                };
            }

            protected override void Update()
            {
                base.Update();

                capClip.Width = Math.Min(cap, DrawWidth);
                bodyClip.Width = Math.Max(0, DrawWidth - cap);

                // The body's rounded corners on the cap side reach under the clip, so only the corners by the handle show.
                body.Width = bodyClip.Width + inner_radius * 2;
                bodyClip.Alpha = DrawWidth > cap ? 1 : 0;
            }
        }
    }
}
