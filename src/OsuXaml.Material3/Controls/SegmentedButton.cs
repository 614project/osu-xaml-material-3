using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's segmented button: two to five options in one outlined pill, for picking one (or with <see cref="IsMultiSelect"/>,
    /// several). Selected segments fill with the secondary container and show a check.
    /// <code>
    /// &lt;m3:SegmentedButton SelectedIndex="{Bind Period}"&gt;
    ///   &lt;m3:Segment Text="Day" /&gt;&lt;m3:Segment Text="Week" /&gt;&lt;m3:Segment Text="Month" /&gt;
    /// &lt;/m3:SegmentedButton&gt;
    /// </code>
    /// The segments share the width equally.
    /// </summary>
    public partial class SegmentedButton : MaterialControl
    {
        private const float height = 40;

        private readonly Container clip;
        private readonly Container dividers;
        private readonly Outline outline;
        private readonly List<Segment> segments = new List<Segment>();
        private bool isMultiSelect;
        private bool updatingSelection;

        public SegmentedButton()
        {
            Height = height;
            Controls.Layout.SetHorizontalAlignment(this, Controls.HorizontalAlignment.Left);

            InternalChildren = new Drawable[]
            {
                clip = new Container { RelativeSizeAxes = Axes.Both, Masking = true },
                dividers = new Container { RelativeSizeAxes = Axes.Both },
                outline = new Outline(),
            };

            SelectedIndex.BindValueChanged(e =>
            {
                if (!isMultiSelect)
                    applySelectedIndex();
            });
        }

        /// <summary>The segments, in order.</summary>
        public IReadOnlyList<Segment> Segments => segments;

        /// <summary>Adds a segment; XAML child elements land here.</summary>
        public void Add(Segment segment)
        {
            segment.Owner = this;

            // A width of its own keeps the segment from sizing itself; this control lays the segments out.
            segment.Width = 1;
            segments.Add(segment);
            clip.Add(segment);
            dividers.Add(new Box { Width = 1, RelativeSizeAxes = Axes.Y });

            segment.IsSelected.BindValueChanged(_ => onSegmentSelectionChanged(segment));

            if (!isMultiSelect && SelectedIndex.Value == segments.Count - 1)
                applySelectedIndex();

            InvalidateMeasure();
        }

        /// <summary>Lets several segments be selected at once; each segment's IsSelected then binds on its own.</summary>
        public bool IsMultiSelect
        {
            get => isMultiSelect;
            set
            {
                isMultiSelect = value;
                if (!value) applySelectedIndex();
            }
        }

        /// <summary>The selected segment of a single-select button, or -1 for none. Binds two-way.</summary>
        public Bindable<int> SelectedIndex { get; } = new Bindable<int>();

        /// <summary>Whether clicking the selected segment of a single-select button deselects it (false by default, as in Material).</summary>
        public bool AllowEmptySelection { get; set; }

        /// <summary>Raised when the selection changes.</summary>
        public event Action SelectionChanged;

        internal void SegmentClicked(Segment segment)
        {
            if (isMultiSelect)
            {
                if (!segment.IsSelected.Disabled)
                    segment.IsSelected.Value = !segment.IsSelected.Value;
                return;
            }

            int index = segments.IndexOf(segment);

            if (SelectedIndex.Value == index)
            {
                if (AllowEmptySelection && !SelectedIndex.Disabled)
                    SelectedIndex.Value = -1;
            }
            else if (!SelectedIndex.Disabled)
                SelectedIndex.Value = index;
        }

        private void applySelectedIndex()
        {
            updatingSelection = true;

            try
            {
                for (int i = 0; i < segments.Count; i++)
                    segments[i].IsSelected.Value = i == SelectedIndex.Value;
            }
            finally
            {
                updatingSelection = false;
            }

            SelectionChanged?.Invoke();
        }

        private void onSegmentSelectionChanged(Segment segment)
        {
            if (updatingSelection)
                return;

            // A segment selected from outside (its own binding) moves a single selection to it.
            if (!isMultiSelect && segment.IsSelected.Value)
                SelectedIndex.Value = segments.IndexOf(segment);
            else
                SelectionChanged?.Invoke();
        }

        private float segmentWidth => segments.Count == 0 ? 0 : segments.Max(s => s.Measure(new Vector2(float.PositiveInfinity)).X);

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(segmentWidth * segments.Count, height);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float radius = DrawHeight / 2;
            clip.CornerRadius = radius;
            outline.ShapeRadius = radius;

            if (segments.Count == 0)
                return;

            float width = DrawWidth / segments.Count;

            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].X = i * width;
                segments[i].Width = width;
                segments[i].Height = DrawHeight;

                segments[i].RoundStart = i == 0;
                segments[i].RoundEnd = i == segments.Count - 1;

                var divider = dividers[i];
                divider.X = i * width - 0.5f;
                divider.Alpha = i == 0 ? 0 : 1;
            }
        }

        protected override void UpdateColors(bool animate)
        {
            Colour4 line = IsEnabled ? Scheme.Outline : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);
            SetColour(outline, line, animate);
            SetColour(dividers, line, animate);
        }

        private bool lastEnabled = true;

        // Disabling the whole button disables (and enabling it enables) every segment.
        protected override void OnStateChanged()
        {
            base.OnStateChanged();

            if (IsEnabled == lastEnabled)
                return;

            lastEnabled = IsEnabled;

            foreach (var segment in segments)
                segment.IsEnabled = IsEnabled;
        }

        protected override bool Interactive => false;
    }

    /// <summary>One option of a <see cref="SegmentedButton"/>: a label and/or an icon. A selected segment shows a check.</summary>
    public partial class Segment : ButtonBase
    {
        private const float icon_size = 18;
        private const float gap = 8;
        private const float padding = 12;

        private readonly Text label;
        private readonly Icon icon;
        private readonly Icon check;
        private readonly Container segmentClip;
        private Symbol symbol;

        public Segment()
        {
            Height = 40;
            Shape = ShapeCorner.None;

            // The end segments round their outer corners. osu!framework's masks don't combine their corners, so the button's
            // rounded clip can't cut a segment's own mask: instead the segment's chrome takes the pill's radius and reaches past
            // its inner edge, where a rectangular mask of the segment's bounds cuts it square.
            RemoveInternal(Chrome, false);
            AddInternal(segmentClip = new Container { RelativeSizeAxes = Axes.Both, Masking = true, Child = Chrome });

            ContentLayer.AddRange(new Drawable[]
            {
                icon = new Icon { Size = icon_size, ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
                check = new Icon(Symbol.Check) { Size = icon_size, ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
                label = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
            });

            IsSelected.BindValueChanged(e =>
            {
                double duration = IsLoaded ? Motion.Short4 : 0;
                check.FadeTo(e.NewValue ? 1 : 0, duration, Motion.Standard);
                icon.FadeTo(!e.NewValue && symbol != Symbol.None ? 1 : 0, duration, Motion.Standard);

                if (e.NewValue && IsLoaded)
                    check.ScaleTo(0.4f).ScaleTo(1, Motion.Medium1, Motion.EmphasizedDecelerate);

                InvalidateMeasure();
                OnStateChanged();
            });
        }

        internal SegmentedButton Owner { get; set; }

        /// <summary>Whether this is the first segment, rounded at its start.</summary>
        internal bool RoundStart { get; set; }

        /// <summary>Whether this is the last segment, rounded at its end.</summary>
        internal bool RoundEnd { get; set; }

        public LocalisableString Text
        {
            get => label.Text;
            set
            {
                label.Text = value;
                InvalidateMeasure();
            }
        }

        public Symbol Icon
        {
            get => symbol;
            set
            {
                symbol = value;
                icon.Symbol = value;
                icon.Alpha = value != Symbol.None && !IsSelected.Value ? 1 : 0;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether the segment is selected. The <see cref="SegmentedButton"/> manages it on clicks.</summary>
        public Bindable<bool> IsSelected { get; } = new Bindable<bool>();

        private bool showsIcon => IsSelected.Value || symbol != Symbol.None;

        private bool hasLabel => !string.IsNullOrEmpty(label.Text.ToString());

        private float contentWidth => (showsIcon ? icon_size : 0) + (showsIcon && hasLabel ? gap : 0) + (hasLabel ? label.DrawWidth : 0);

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(Math.Max(48, padding * 2 + contentWidth), 40);

        protected override void Update()
        {
            base.Update();

            float radius = RoundStart || RoundEnd ? DrawHeight / 2 : 0;
            float reachStart = RoundEnd && !RoundStart ? radius : 0;
            float reachEnd = RoundStart && !RoundEnd ? radius : 0;

            Chrome.CornerRadius = radius;
            segmentClip.Padding = new MarginPadding { Left = -reachStart, Right = -reachEnd };
            ContentLayer.Padding = new MarginPadding { Left = reachStart, Right = reachEnd };
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float x = (DrawWidth - contentWidth) / 2;
            icon.X = check.X = x;
            label.X = x + (showsIcon ? icon_size + gap : 0);
        }

        protected override void OnClicked() => Owner?.SegmentClicked(this);

        protected override void UpdateColors(bool animate)
        {
            bool selected = IsSelected.Value;
            ColorRole content = selected ? ColorRole.OnSecondaryContainer : ColorRole.OnSurface;

            Colour4 container = !selected ? Colour4.Transparent
                : IsEnabled ? Scheme.SecondaryContainer : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);

            ApplyChrome(container, Scheme[content], null, 0, animate);
            SetColour(label, ContentColour(content), animate);
            SetColour(icon, ContentColour(content), animate);
            SetColour(check, ContentColour(content), animate);
        }
    }
}
