using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>The two kinds of <see cref="Tabs"/>.</summary>
    public enum TabsVariant
    {
        /// <summary>Main destinations, below a top app bar: icons above labels, a short indicator under the content.</summary>
        Primary,

        /// <summary>Related content within a page: icons beside labels, an indicator the width of the tab.</summary>
        Secondary,
    }

    /// <summary>
    /// Material's tabs: a row of equal-width tabs with an indicator that slides to the selected one, and the selected tab's content
    /// below (when the tabs have content).
    /// <code>
    /// &lt;m3:Tabs SelectedIndex="{Bind Tab}"&gt;
    ///   &lt;m3:Tab Text="Flights" Icon="Flight"&gt;&lt;StackPanel&gt;...&lt;/StackPanel&gt;&lt;/m3:Tab&gt;
    ///   &lt;m3:Tab Text="Hotels" Icon="Hotel" /&gt;
    /// &lt;/m3:Tabs&gt;
    /// </code>
    /// </summary>
    public partial class Tabs : MaterialControl
    {
        private readonly Box background;
        private readonly Container<Tab> row;
        private readonly Box divider;
        private readonly Container indicatorClip;
        private readonly Container indicator;
        private readonly ContentPresenter presenter;
        private readonly List<Tab> tabs = new List<Tab>();
        private TabsVariant variant;

        // Where the indicator is drawn, gliding to where it should be (the target) when the selection changes.
        private float indicatorLeft;
        private float indicatorRight;
        private float targetLeft = float.NaN;
        private float targetRight = float.NaN;

        public Tabs()
        {
            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.X },
                row = new Container<Tab> { RelativeSizeAxes = Axes.X },
                divider = new Box { RelativeSizeAxes = Axes.X, Height = 1 },
                indicatorClip = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Masking = true,
                    Child = indicator = new Container
                    {
                        Masking = true,
                        Child = new Box { RelativeSizeAxes = Axes.Both },
                    },
                },
                presenter = new ContentPresenter { Width = 1, Height = 1 },
            };

            SelectedIndex.BindValueChanged(_ => applySelection(true));
        }

        public TabsVariant Variant
        {
            get => variant;
            set
            {
                variant = value;

                foreach (var tab in tabs)
                    tab.Variant = value;

                InvalidateMeasure();
                OnStateChanged();
            }
        }

        /// <summary>The selected tab. Binds two-way.</summary>
        public Bindable<int> SelectedIndex { get; } = new Bindable<int>();

        public IReadOnlyList<Tab> Items => tabs;

        public void Add(Tab tab)
        {
            tab.Owner = this;
            tab.Variant = variant;
            tabs.Add(tab);
            row.Add(tab);
            applySelection(false);
            InvalidateMeasure();
        }

        internal void TabClicked(Tab tab)
        {
            if (!SelectedIndex.Disabled)
                SelectedIndex.Value = tabs.IndexOf(tab);
        }

        private Tab selected => SelectedIndex.Value >= 0 && SelectedIndex.Value < tabs.Count ? tabs[SelectedIndex.Value] : null;

        private void applySelection(bool animate)
        {
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].IsSelected = i == SelectedIndex.Value;

            presenter.Content = selected?.Child;
            animateIndicator = animate && IsLoaded;
            InvalidateMeasure();
        }

        private bool animateIndicator;

        private bool hasIcons => tabs.Exists(t => t.Icon != Symbol.None);

        private bool hasText => tabs.Exists(t => !string.IsNullOrEmpty(t.Text.ToString()));

        private float barHeight => variant == TabsVariant.Primary && hasIcons && hasText ? 64 : 48;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            float tabWidth = 0;
            foreach (var tab in tabs)
                tabWidth = Math.Max(tabWidth, tab.DesiredWidth);

            var content = presenter.Content != null
                ? presenter.Measure(new Vector2(availableSize.X, Math.Max(0, availableSize.Y - barHeight)))
                : Vector2.Zero;

            return new Vector2(Math.Max(tabWidth * tabs.Count, content.X), barHeight + content.Y);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float bar = barHeight;
            background.Height = bar;
            row.Height = bar;
            divider.Y = bar - 1;

            presenter.Y = bar;
            presenter.Width = DrawWidth;
            presenter.Height = Math.Max(0, DrawHeight - bar);
            presenter.Alpha = presenter.Content != null ? 1 : 0;

            if (tabs.Count == 0)
                return;

            float width = DrawWidth / tabs.Count;

            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].X = i * width;
                tabs[i].Size = new Vector2(width, bar);
            }

            var current = selected;

            if (current == null)
            {
                indicator.Alpha = 0;
                return;
            }

            indicator.Alpha = 1;

            // Primary tabs underline the content (at least 24dp); secondary tabs underline the whole tab.
            float left, right;

            if (variant == TabsVariant.Primary)
            {
                float content = Math.Max(24, current.ContentWidth);
                left = current.X + (width - content) / 2;
                right = left + content;
            }
            else
            {
                left = current.X;
                right = current.X + width;
            }

            if (left != targetLeft || right != targetRight)
            {
                bool glide = animateIndicator && !float.IsNaN(targetLeft);
                targetLeft = left;
                targetRight = right;

                if (glide)
                {
                    this.TransformTo(nameof(indicatorLeft), left, Motion.Medium2, Motion.EmphasizedDecelerate);
                    this.TransformTo(nameof(indicatorRight), right, Motion.Medium2, Motion.EmphasizedDecelerate);
                }
                else
                {
                    ClearTransforms(false, nameof(indicatorLeft));
                    ClearTransforms(false, nameof(indicatorRight));
                    indicatorLeft = left;
                    indicatorRight = right;
                }
            }

            animateIndicator = false;

            bool primary = variant == TabsVariant.Primary;
            float thickness = primary ? 3 : 2;

            indicator.X = indicatorLeft;
            indicator.Width = Math.Max(0, indicatorRight - indicatorLeft);
            indicatorClip.Height = bar;
            indicator.Y = bar - thickness;

            // Primary indicators have rounded top corners: a taller rounded box cut off at the bar's bottom.
            indicator.Height = primary ? thickness * 2 : thickness;
            indicator.CornerRadius = primary ? thickness : 0;
        }

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
            SetColour(background, Scheme.Surface, animate);
            SetColour(divider, Scheme.SurfaceVariant, animate);
            SetColour(indicator, Scheme.Primary, animate);
        }
    }

    /// <summary>One tab of <see cref="Tabs"/>: a label, an icon or both, and optionally the content shown while it is selected.</summary>
    public partial class Tab : MaterialControl
    {
        private const float icon_size = 24;

        private readonly StateLayer stateLayer;
        private readonly Icon icon;
        private readonly Text label;
        private readonly Badge badge;
        private bool isSelected;
        private TabsVariant variant;
        private Symbol symbol;

        public Tab()
        {
            Width = 1;
            Height = 48;

            InternalChildren = new Drawable[]
            {
                stateLayer = new StateLayer(),
                icon = new Icon { ColorRole = null, Alpha = 0 },
                label = new Text(TypeStyle.TitleSmall) { ColorRole = null },
                badge = new Badge { Alpha = 0 },
            };
        }

        internal Tabs Owner { get; set; }

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
                icon.Alpha = value == Symbol.None ? 0 : 1;
                InvalidateMeasure();
            }
        }

        /// <summary>A badge on the icon (or after the label): a dot, or this text.</summary>
        public LocalisableString BadgeText
        {
            get => badge.Text;
            set
            {
                badge.Text = value;
                badge.Alpha = 1;
            }
        }

        /// <summary>What the <see cref="Tabs"/> show below while this tab is selected; XAML content lands here.</summary>
        public Drawable Child { get; set; }

        internal bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                icon.Filled = value;
                OnStateChanged();
            }
        }

        internal TabsVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                OnStateChanged();
            }
        }

        private bool hasIcon => symbol != Symbol.None;

        private bool hasLabel => !string.IsNullOrEmpty(label.Text.ToString());

        private bool stacked => variant == TabsVariant.Primary && hasIcon && hasLabel;

        /// <summary>The width of the icon and label as laid out.</summary>
        internal float ContentWidth => stacked ? Math.Max(icon_size, label.DrawWidth)
            : (hasIcon ? icon_size : 0) + (hasIcon && hasLabel ? 8 : 0) + (hasLabel ? label.DrawWidth : 0);

        internal float DesiredWidth => Math.Max(90, ContentWidth + 32);

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(DesiredWidth, stacked ? 64 : 48);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (stacked)
            {
                icon.Anchor = icon.Origin = Anchor.TopCentre;
                icon.Position = new Vector2(0, 10);
                label.Anchor = label.Origin = Anchor.TopCentre;
                label.Position = new Vector2(0, 10 + icon_size + 2);
            }
            else
            {
                float x = (DrawWidth - ContentWidth) / 2;
                icon.Anchor = icon.Origin = Anchor.CentreLeft;
                icon.Position = new Vector2(x, 0);
                label.Anchor = label.Origin = Anchor.CentreLeft;
                label.Position = new Vector2(x + (hasIcon ? icon_size + 8 : 0), 0);
            }

            if (badge.Alpha > 0)
                Material3.Badge.PlaceOn(badge, hasIcon ? icon : label);
        }

        protected override void OnPressed(Vector2 screenSpacePosition) => stateLayer.Press(screenSpacePosition);

        protected override void OnReleased() => stateLayer.Release();

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsEnabled)
                return false;

            Owner?.TabClicked(this);
            return true;
        }

        protected override void UpdateColors(bool animate)
        {
            ColorRole active = variant == TabsVariant.Primary ? ColorRole.Primary : ColorRole.OnSurface;
            ColorRole role = isSelected ? active : ColorRole.OnSurfaceVariant;

            SetColour(icon, ContentColour(role), animate);
            SetColour(label, ContentColour(role), animate);

            stateLayer.Color = Scheme[isSelected ? active : ColorRole.OnSurface];
            stateLayer.Hovered = IsHovered;
            stateLayer.Enabled = IsEnabled;
        }
    }
}
