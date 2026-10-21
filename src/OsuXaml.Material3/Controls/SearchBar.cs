using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's search bar: a 56dp pill with a leading icon, a placeholder and trailing elements (an icon button, an avatar).
    /// <code>
    /// &lt;m3:SearchBar Placeholder="Search mail" Text="{Bind Query}" Committed="Search"&gt;
    ///   &lt;m3:Avatar Text="JK" Diameter="30" /&gt;
    /// &lt;/m3:SearchBar&gt;
    /// </code>
    /// </summary>
    public partial class SearchBar : MaterialControl
    {
        private const float height = 56;

        private readonly Container clip;
        private readonly Box background;
        private readonly Box hoverLayer;
        private readonly IconButton leadingButton;
        private readonly Container inputHolder;
        private readonly MaterialTextInput input;
        private readonly FillFlowContainer trailing;

        public SearchBar()
        {
            Height = height;

            InternalChildren = new Drawable[]
            {
                clip = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box { RelativeSizeAxes = Axes.Both },
                        hoverLayer = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                    },
                },
                leadingButton = new IconButton { Icon = Symbol.Search, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, X = 8 },
                inputHolder = new Container { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Height = 24, X = 56 },
                trailing = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    X = -8,
                },
            };

            inputHolder.Add(input = new MaterialTextInput(false, 24) { RelativeSizeAxes = Axes.X });
            input.Current.BindTo(Text);
            input.OnCommit += (_, _) => Committed?.Invoke(Text.Value);
            leadingButton.Click += () => LeadingClick?.Invoke();
        }

        /// <summary>The query. Binds two-way.</summary>
        public Bindable<string> Text { get; } = new Bindable<string>(string.Empty);

        public LocalisableString Placeholder
        {
            get => input.Hint;
            set => input.Hint = value;
        }

        /// <summary>The icon at the start; Search by default (Menu or ArrowBack are common too).</summary>
        public Symbol LeadingIcon
        {
            get => leadingButton.Icon;
            set => leadingButton.Icon = value;
        }

        /// <summary>Raised when the leading icon is clicked.</summary>
        public event Action LeadingClick;

        /// <summary>Raised when Enter is pressed, with the query.</summary>
        public event Action<string> Committed;

        /// <summary>Adds an element at the end (an icon button, an avatar); XAML child elements land here.</summary>
        public void Add(Drawable drawable) => trailing.Add(drawable);

        /// <summary>Gives the search bar keyboard focus.</summary>
        public void Focus() => Schedule(() => GetContainingFocusManager()?.ChangeFocus(input));

        public override bool AcceptsFocus => false;

        protected override bool OnClick(ClickEvent e)
        {
            if (IsEnabled && !input.HasFocus)
                GetContainingFocusManager()?.ChangeFocus(input);

            return true;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(360, height);

        protected override void Update()
        {
            base.Update();
            input.ReadOnlyByDisabled = !IsEnabled;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            clip.CornerRadius = DrawHeight / 2;
            float end = trailing.Count > 0 ? trailing.DrawWidth + 8 + 8 : 16;
            inputHolder.Width = Math.Max(0, DrawWidth - inputHolder.X - end);
        }

        protected override void UpdateColors(bool animate)
        {
            SetColour(background, Scheme.SurfaceContainerHigh, animate);
            hoverLayer.Colour = Scheme.OnSurface;
            hoverLayer.FadeTo(IsHovered && IsEnabled ? StateOpacity.Hover : 0, Motion.Short3);
            leadingButton.IconColor = Scheme.OnSurface;
            leadingButton.IsEnabled = IsEnabled;
            input.ApplyColours(ContentColour(ColorRole.OnSurface), Scheme.OnSurfaceVariant, Scheme.Primary);
        }
    }
}
