using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>The sizes of <see cref="TopAppBar"/>.</summary>
    public enum TopAppBarVariant
    {
        /// <summary>64dp, the title beside the navigation icon.</summary>
        Small,

        /// <summary>64dp, the title centred.</summary>
        CenterAligned,

        /// <summary>112dp, a headline-small title on a second row.</summary>
        Medium,

        /// <summary>152dp, a headline-medium title on a second row.</summary>
        Large,
    }

    /// <summary>
    /// Material's top app bar: a navigation icon, a title and action buttons at the top of a screen.
    /// <code>
    /// &lt;m3:TopAppBar Title="Inbox" NavigationIcon="Menu" Variant="Medium"&gt;
    ///   &lt;m3:IconButton Icon="Search" /&gt;&lt;m3:IconButton Icon="MoreVert" /&gt;
    /// &lt;/m3:TopAppBar&gt;
    /// </code>
    /// Set <see cref="IsScrolled"/> while content scrolls under it: the bar then takes the SurfaceContainer colour.
    /// </summary>
    public partial class TopAppBar : MaterialControl
    {
        private readonly Box background;
        private readonly IconButton navigationButton;
        private readonly Text title;
        private readonly FillFlowContainer actions;
        private TopAppBarVariant variant;
        private bool isScrolled;

        public TopAppBar()
        {
            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                navigationButton = new IconButton { X = 8, Y = 12, Alpha = 0 },
                title = new Text(TypeStyle.TitleLarge) { ColorRole = null },
                actions = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(0),
                    X = -8,
                    Y = 12,
                },
            };

            navigationButton.Click += () => NavigationClick?.Invoke();
            applyVariant();
        }

        public TopAppBarVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                applyVariant();
            }
        }

        public LocalisableString Title
        {
            get => title.Text;
            set => title.Text = value;
        }

        /// <summary>The icon of the button at the start (Menu or ArrowBack); None for no button.</summary>
        public Symbol NavigationIcon
        {
            get => navigationButton.Icon;
            set
            {
                navigationButton.Icon = value;
                navigationButton.Alpha = value == Symbol.None ? 0 : 1;
            }
        }

        /// <summary>Raised when the navigation button is clicked.</summary>
        public event Action NavigationClick;

        /// <summary>Whether content is scrolled under the bar; it then fills with SurfaceContainer to stand apart.</summary>
        public bool IsScrolled
        {
            get => isScrolled;
            set
            {
                isScrolled = value;
                OnStateChanged();
            }
        }

        /// <summary>Adds an action at the end of the bar (usually an <see cref="IconButton"/>); XAML child elements land here.</summary>
        public void Add(Drawable action) => actions.Add(action);

        private float barHeight => variant switch
        {
            TopAppBarVariant.Medium => 112,
            TopAppBarVariant.Large => 152,
            _ => 64,
        };

        private void applyVariant()
        {
            Typescale.Apply(title, variant switch
            {
                TopAppBarVariant.Medium => TypeStyle.HeadlineSmall,
                TopAppBarVariant.Large => TypeStyle.HeadlineMedium,
                _ => TypeStyle.TitleLarge,
            });

            InvalidateMeasure();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(360, barHeight);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            bool hasNavigation = navigationButton.Alpha > 0;

            switch (variant)
            {
                case TopAppBarVariant.CenterAligned:
                    title.Anchor = title.Origin = Anchor.TopCentre;
                    title.Position = new Vector2(0, 32 - title.DrawHeight / 2);
                    break;

                case TopAppBarVariant.Medium:
                case TopAppBarVariant.Large:
                    title.Anchor = title.Origin = Anchor.BottomLeft;
                    title.Position = new Vector2(16, variant == TopAppBarVariant.Medium ? -20 : -24);
                    break;

                default:
                    title.Anchor = title.Origin = Anchor.TopLeft;
                    title.Position = new Vector2(hasNavigation ? 56 : 16, 32 - title.DrawHeight / 2);
                    break;
            }
        }

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
            SetColour(background, isScrolled ? Scheme.SurfaceContainer : Scheme.Surface, animate);
            SetColour(title, Scheme.OnSurface, animate);
        }
    }

    /// <summary>
    /// Material's bottom app bar: up to four actions along the bottom of a compact screen and an optional FAB at the end.
    /// <code>
    /// &lt;m3:BottomAppBar&gt;
    ///   &lt;m3:IconButton Icon="Check" /&gt;&lt;m3:IconButton Icon="Edit" /&gt;
    ///   &lt;m3:BottomAppBar.Fab&gt;&lt;m3:Fab Icon="Add" Color="Secondary" /&gt;&lt;/m3:BottomAppBar.Fab&gt;
    /// &lt;/m3:BottomAppBar&gt;
    /// </code>
    /// </summary>
    public partial class BottomAppBar : MaterialControl
    {
        private const float height = 80;

        private readonly Box background;
        private readonly FillFlowContainer actions;
        private readonly Container fabHolder;
        private Drawable fab;

        public BottomAppBar()
        {
            Height = height;

            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                actions = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    X = 12,
                },
                fabHolder = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    X = -16,
                },
            };
        }

        /// <summary>Adds an action (usually an <see cref="IconButton"/>); XAML child elements land here.</summary>
        public void Add(Drawable action) => actions.Add(action);

        /// <summary>A FAB at the end of the bar. A <see cref="Material3.Fab"/> placed here rests flat on the bar, as Material draws it.</summary>
        public Drawable Fab
        {
            get => fab;
            set
            {
                if (fab != null)
                    fabHolder.Remove(fab, true);

                fab = value;

                if (fab is Fab f)
                    f.Lowered = true;

                if (fab != null)
                    fabHolder.Add(fab);
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(360, height);

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate) => SetColour(background, Scheme.SurfaceContainer, animate);
    }
}
