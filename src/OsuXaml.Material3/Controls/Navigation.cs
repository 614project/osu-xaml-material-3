using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What navigation destinations share: an icon (filled while selected, or <see cref="SelectedIcon"/>), a label, a badge, and a
    /// pill-shaped active indicator. The bar, rail or drawer they are in selects them on click.
    /// </summary>
    public abstract partial class NavigationItem : MaterialControl
    {
        protected readonly Container IndicatorClip;
        protected readonly Box IndicatorFill;
        protected readonly StateLayer StateLayer;
        protected readonly Icon IconDrawable;
        protected readonly Text Label;
        protected readonly Badge Badge;

        private Symbol icon;
        private Symbol selectedIcon;
        private bool showBadge;

        protected NavigationItem(TypeStyle labelStyle)
        {
            InternalChildren = new Drawable[]
            {
                IndicatorClip = new Container
                {
                    Masking = true,
                    Children = new Drawable[]
                    {
                        IndicatorFill = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                        StateLayer = new StateLayer(),
                    },
                },
                IconDrawable = new Icon { ColorRole = null, Origin = Anchor.Centre },
                Label = new Text(labelStyle) { ColorRole = null },
                Badge = new Badge { Alpha = 0 },
            };

            IsSelected.BindValueChanged(e =>
            {
                applyIcon();
                OnSelectionChanged(e.NewValue, IsLoaded);
                OnStateChanged();
            });
        }

        public Symbol Icon
        {
            get => icon;
            set
            {
                icon = value;
                applyIcon();
            }
        }

        /// <summary>The icon while selected; by default the same icon, filled.</summary>
        public Symbol SelectedIcon
        {
            get => selectedIcon;
            set
            {
                selectedIcon = value;
                applyIcon();
            }
        }

        public LocalisableString Text
        {
            get => Label.Text;
            set
            {
                Label.Text = value;
                InvalidateMeasure();
            }
        }

        /// <summary>A badge with this text on the icon (a count, say).</summary>
        public LocalisableString BadgeText
        {
            get => Badge.Text;
            set
            {
                Badge.Text = value;
                applyBadge();
            }
        }

        /// <summary>Shows a badge: a dot, or the <see cref="BadgeText"/>.</summary>
        public bool ShowBadge
        {
            get => showBadge;
            set
            {
                showBadge = value;
                applyBadge();
            }
        }

        /// <summary>Whether this is the current destination. Its bar, rail or drawer sets it.</summary>
        public Bindable<bool> IsSelected { get; } = new Bindable<bool>();

        /// <summary>Raised when the item is clicked, after it is selected.</summary>
        public event Action Click;

        internal Action<NavigationItem> Selected;

        private void applyIcon()
        {
            IconDrawable.Symbol = IsSelected.Value && selectedIcon != Symbol.None ? selectedIcon : icon;
            IconDrawable.Filled = IsSelected.Value;
        }

        private void applyBadge() => Badge.Alpha = showBadge || !string.IsNullOrEmpty(Badge.Text.ToString()) ? 1 : 0;

        protected virtual void OnSelectionChanged(bool selected, bool animate)
        {
        }

        protected override void OnPressed(Vector2 screenSpacePosition) => StateLayer.Press(screenSpacePosition);

        protected override void OnReleased() => StateLayer.Release();

        protected override bool OnClick(osu.Framework.Input.Events.ClickEvent e)
        {
            if (!IsEnabled)
                return false;

            Selected?.Invoke(this);
            Click?.Invoke();
            return true;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            Badge.PlaceOn(Badge, IconDrawable);
        }

        /// <summary>The roles of the icon and label, selected or not.</summary>
        protected abstract (ColorRole Icon, ColorRole Label) ContentRoles(bool selected);

        protected override void UpdateColors(bool animate)
        {
            bool selected = IsSelected.Value;
            var roles = ContentRoles(selected);

            SetColour(IndicatorFill, Scheme.SecondaryContainer, animate);
            SetColour(IconDrawable, ContentColour(roles.Icon), animate);
            SetColour(Label, ContentColour(roles.Label), animate);

            StateLayer.Color = Scheme[selected ? ColorRole.OnSurface : ColorRole.OnSurfaceVariant];
            StateLayer.Hovered = IsHovered;
            StateLayer.Enabled = IsEnabled;
        }
    }

    /// <summary>
    /// What the bar, rail and drawer share: their destinations and <see cref="SelectedIndex"/>, which binds two-way.
    /// </summary>
    public abstract partial class NavigationHost : MaterialControl
    {
        private readonly List<NavigationItem> items = new List<NavigationItem>();
        private bool selecting;

        protected NavigationHost()
        {
            SelectedIndex.BindValueChanged(_ => applySelection());
        }

        /// <summary>The selected destination, or -1 for none. Binds two-way.</summary>
        public Bindable<int> SelectedIndex { get; } = new Bindable<int>();

        public IReadOnlyList<NavigationItem> Items => items;

        /// <summary>Raised when a destination is clicked, with its index (also when it was already selected).</summary>
        public event Action<int> ItemClicked;

        protected void AddItem(NavigationItem item)
        {
            items.Add(item);
            item.Selected = select;
            item.IsSelected.Value = items.Count - 1 == SelectedIndex.Value;
            InvalidateMeasure();
        }

        private void select(NavigationItem item)
        {
            int index = items.IndexOf(item);

            if (!SelectedIndex.Disabled)
                SelectedIndex.Value = index;

            ItemClicked?.Invoke(index);
        }

        private void applySelection()
        {
            if (selecting)
                return;

            selecting = true;

            for (int i = 0; i < items.Count; i++)
                items[i].IsSelected.Value = i == SelectedIndex.Value;

            selecting = false;
        }
    }

    /// <summary>
    /// Material's navigation bar: three to five destinations across the bottom of a compact window.
    /// <code>
    /// &lt;m3:NavigationBar SelectedIndex="{Bind Page}"&gt;
    ///   &lt;m3:NavigationBarItem Icon="Home" Text="Home" /&gt;&lt;m3:NavigationBarItem Icon="Search" Text="Search" BadgeText="3" /&gt;
    /// &lt;/m3:NavigationBar&gt;
    /// </code>
    /// </summary>
    public partial class NavigationBar : NavigationHost
    {
        private const float height = 80;

        private readonly Box background;
        private readonly Container<NavigationBarItem> row;

        public NavigationBar()
        {
            Height = height;

            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                row = new Container<NavigationBarItem> { RelativeSizeAxes = Axes.Both },
            };
        }

        public void Add(NavigationBarItem item)
        {
            row.Add(item);
            AddItem(item);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(Math.Max(1, row.Count) * 96, height);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (row.Count == 0)
                return;

            float width = DrawWidth / row.Count;

            for (int i = 0; i < row.Count; i++)
            {
                row[i].X = i * width;
                row[i].Size = new Vector2(width, DrawHeight);
            }
        }

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate) => SetColour(background, Scheme.SurfaceContainer, animate);
    }

    /// <summary>A destination in a <see cref="NavigationBar"/>: a 64dp indicator around the icon and a label below.</summary>
    public partial class NavigationBarItem : NavigationItem
    {
        public NavigationBarItem()
            : base(TypeStyle.LabelMedium)
        {
            Width = 1;
            Height = 80;

            IndicatorClip.Size = new Vector2(64, 32);
            IndicatorClip.CornerRadius = 16;
            IndicatorClip.Anchor = Anchor.TopCentre;
            IndicatorClip.Origin = Anchor.TopCentre;
            IndicatorClip.Y = 12;

            IconDrawable.Anchor = Anchor.TopCentre;
            IconDrawable.Y = 12 + 16;

            Label.Anchor = Anchor.TopCentre;
            Label.Origin = Anchor.TopCentre;
            Label.Y = 12 + 32 + 4;
        }

        protected override void OnSelectionChanged(bool selected, bool animate)
        {
            // The indicator grows out from the icon's centre.
            IndicatorFill.FadeTo(selected ? 1 : 0, animate ? Motion.Short2 : 0);

            if (selected)
                IndicatorFill.ResizeWidthTo(0.4f).ResizeWidthTo(1, animate ? Motion.Medium2 : 0, Motion.EmphasizedDecelerate);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(Math.Max(64, Label.DrawWidth) + 16, 80);

        protected override (ColorRole Icon, ColorRole Label) ContentRoles(bool selected)
            => selected ? (ColorRole.OnSecondaryContainer, ColorRole.OnSurface) : (ColorRole.OnSurfaceVariant, ColorRole.OnSurfaceVariant);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            IndicatorFill.Anchor = IndicatorFill.Origin = Anchor.Centre;
        }
    }

    /// <summary>How a <see cref="NavigationRail"/> places its destinations.</summary>
    public enum NavigationRailAlignment
    {
        Top,
        Center,
        Bottom,
    }

    /// <summary>
    /// Material's navigation rail: destinations down the side of a medium window, with an optional <see cref="Header"/> (a menu
    /// button or a FAB) above them. <c>&lt;m3:NavigationRail SelectedIndex="{Bind Page}"&gt;&lt;m3:NavigationRailItem Icon="Home" Text="Home" /&gt;...</c>
    /// </summary>
    public partial class NavigationRail : NavigationHost
    {
        private const float width = 80;
        private const float item_spacing = 12;

        private readonly Box background;
        private readonly Container headerHolder;
        private readonly FillFlowContainer<NavigationRailItem> column;
        private Drawable header;

        public NavigationRail()
        {
            Width = width;

            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                headerHolder = new Container { RelativeSizeAxes = Axes.X, AutoSizeAxes = Axes.Y, Y = 12 },
                column = new FillFlowContainer<NavigationRailItem>
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, item_spacing),
                },
            };
        }

        /// <summary>A menu button or FAB above the destinations.</summary>
        public Drawable Header
        {
            get => header;
            set
            {
                if (header != null)
                    headerHolder.Remove(header, true);

                header = value;

                if (value != null)
                {
                    value.Anchor = Anchor.TopCentre;
                    value.Origin = Anchor.TopCentre;
                    headerHolder.Add(value);
                }

                InvalidateMeasure();
            }
        }

        public NavigationRailAlignment Alignment { get; set; }

        public void Add(NavigationRailItem item)
        {
            column.Add(item);
            AddItem(item);
        }

        private float itemsTop => header != null ? 12 + headerHolder.DrawHeight + 40 : 12;

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(width, itemsTop + column.DrawHeight + 12);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float free = DrawHeight - itemsTop - column.DrawHeight - 12;
            column.Y = itemsTop + Alignment switch
            {
                NavigationRailAlignment.Center => Math.Max(0, free / 2),
                NavigationRailAlignment.Bottom => Math.Max(0, free),
                _ => 0,
            };
        }

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate) => SetColour(background, Scheme.Surface, animate);
    }

    /// <summary>A destination in a <see cref="NavigationRail"/>: a 56dp indicator around the icon and a label below.</summary>
    public partial class NavigationRailItem : NavigationItem
    {
        public NavigationRailItem()
            : base(TypeStyle.LabelMedium)
        {
            RelativeSizeAxes = Axes.X;
            Height = 56;

            IndicatorClip.Size = new Vector2(56, 32);
            IndicatorClip.CornerRadius = 16;
            IndicatorClip.Anchor = Anchor.TopCentre;
            IndicatorClip.Origin = Anchor.TopCentre;

            IconDrawable.Anchor = Anchor.TopCentre;
            IconDrawable.Y = 16;

            Label.Anchor = Anchor.TopCentre;
            Label.Origin = Anchor.TopCentre;
            Label.Y = 32 + 4;
        }

        protected override void OnSelectionChanged(bool selected, bool animate)
        {
            IndicatorFill.FadeTo(selected ? 1 : 0, animate ? Motion.Short2 : 0);

            if (selected)
                IndicatorFill.ResizeWidthTo(0.4f).ResizeWidthTo(1, animate ? Motion.Medium2 : 0, Motion.EmphasizedDecelerate);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(80, 56);

        protected override (ColorRole Icon, ColorRole Label) ContentRoles(bool selected)
            => selected ? (ColorRole.OnSecondaryContainer, ColorRole.OnSurface) : (ColorRole.OnSurfaceVariant, ColorRole.OnSurfaceVariant);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            IndicatorFill.Anchor = IndicatorFill.Origin = Anchor.Centre;
        }
    }

    /// <summary>
    /// Material's navigation drawer (standard): a 360dp column of destinations with section headlines and dividers between them.
    /// <code>
    /// &lt;m3:NavigationDrawer SelectedIndex="{Bind Folder}"&gt;
    ///   &lt;m3:NavigationDrawerHeadline Text="Mail" /&gt;
    ///   &lt;m3:NavigationDrawerItem Icon="Inbox" Text="Inbox" BadgeText="24" /&gt;
    ///   &lt;m3:Divider /&gt;
    /// &lt;/m3:NavigationDrawer&gt;
    /// </code>
    /// <see cref="NavigationHost.SelectedIndex"/> counts destinations only.
    /// </summary>
    public partial class NavigationDrawer : NavigationHost
    {
        private const float width = 360;
        private const float padding = 12;

        private readonly Box background;
        private readonly FillFlowContainer column;

        public NavigationDrawer()
        {
            Width = width;

            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                column = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(padding),
                },
            };
        }

        /// <summary>Adds a destination, a headline, a divider or anything else; XAML child elements land here.</summary>
        public void Add(Drawable drawable)
        {
            if (drawable is Divider divider)
            {
                divider.InsetStart = divider.InsetEnd = 16;
                divider.Margin = new MarginPadding { Vertical = 8 };
            }

            column.Add(drawable);

            if (drawable is NavigationItem item)
                AddItem(item);

            InvalidateMeasure();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(width, column.DrawHeight);

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate) => SetColour(background, Scheme.SurfaceContainerLow, animate);
    }

    /// <summary>A destination in a <see cref="NavigationDrawer"/>: a 56dp row with an icon, a label and a trailing badge text.</summary>
    public partial class NavigationDrawerItem : NavigationItem
    {
        public NavigationDrawerItem()
            : base(TypeStyle.LabelLarge)
        {
            RelativeSizeAxes = Axes.X;
            Height = 56;

            IndicatorClip.RelativeSizeAxes = Axes.Both;
            IndicatorClip.CornerRadius = 28;

            IconDrawable.Anchor = Anchor.CentreLeft;
            IconDrawable.X = 16 + 12;

            Label.Anchor = Anchor.CentreLeft;
            Label.Origin = Anchor.CentreLeft;
            Label.X = 16 + 24 + 12;

            // The drawer shows badge text as plain text at the end of the row.
            Badge.Alpha = 0;
            AddInternal(trailing = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, X = -24 });
        }

        private readonly Text trailing;

        /// <summary>Text at the end of the row, such as an unread count.</summary>
        public new LocalisableString BadgeText
        {
            get => trailing.Text;
            set => trailing.Text = value;
        }

        protected override void OnSelectionChanged(bool selected, bool animate) => IndicatorFill.FadeTo(selected ? 1 : 0, animate ? Motion.Short4 : 0);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            Badge.Alpha = 0;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(336, 56);

        protected override (ColorRole Icon, ColorRole Label) ContentRoles(bool selected)
            => selected ? (ColorRole.OnSecondaryContainer, ColorRole.OnSecondaryContainer) : (ColorRole.OnSurfaceVariant, ColorRole.OnSurfaceVariant);

        protected override void UpdateColors(bool animate)
        {
            base.UpdateColors(animate);
            SetColour(trailing, ContentColour(ContentRoles(IsSelected.Value).Label), animate);
        }
    }

    /// <summary>A section title in a <see cref="NavigationDrawer"/>.</summary>
    public partial class NavigationDrawerHeadline : Text
    {
        public NavigationDrawerHeadline()
            : base(TypeStyle.TitleSmall)
        {
            ColorRole = Material3.ColorRole.OnSurfaceVariant;
            Margin = new MarginPadding { Left = 16, Top = 18, Bottom = 18 };
        }
    }
}
