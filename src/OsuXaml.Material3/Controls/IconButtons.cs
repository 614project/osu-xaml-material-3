using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    public enum IconButtonVariant
    {
        /// <summary>Just the icon, for the most compact layouts (app bars, list rows).</summary>
        Standard,

        /// <summary>A primary container, for the most important icon action.</summary>
        Filled,

        /// <summary>A secondary container, between filled and outlined.</summary>
        FilledTonal,

        /// <summary>An outline, for secondary icon actions.</summary>
        Outlined,
    }

    /// <summary>
    /// A 40dp circular button with a 24dp icon: <c>&lt;m3:IconButton Icon="Favorite" Variant="FilledTonal" /&gt;</c>.
    /// With <see cref="IsToggle"/> it flips <see cref="IsSelected"/> on click and shows the selected colours, and the icon turns
    /// filled while selected (or becomes <see cref="SelectedIcon"/>).
    /// </summary>
    public partial class IconButton : ButtonBase
    {
        private const float size = 40;

        private readonly Icon icon;
        private Symbol symbol;
        private Symbol selectedSymbol;
        private IconButtonVariant variant;

        public IconButton()
        {
            Size = new Vector2(size);
            ContentLayer.Add(icon = new Icon { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre });

            IsSelected.BindValueChanged(_ =>
            {
                applyIcon();
                OnStateChanged();
            });
        }

        public Symbol Icon
        {
            get => symbol;
            set
            {
                symbol = value;
                applyIcon();
            }
        }

        /// <summary>The icon while selected; by default the same icon, filled.</summary>
        public Symbol SelectedIcon
        {
            get => selectedSymbol;
            set
            {
                selectedSymbol = value;
                applyIcon();
            }
        }

        public IconButtonVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                OnStateChanged();
            }
        }

        /// <summary>Overrides the icon's colour (an error colour in a text field, say); null follows the variant.</summary>
        public Colour4? IconColor
        {
            get => iconColor;
            set
            {
                iconColor = value;
                OnStateChanged();
            }
        }

        private Colour4? iconColor;

        /// <summary>Makes the button a toggle: clicks flip <see cref="IsSelected"/>.</summary>
        public bool IsToggle { get; set; }

        /// <summary>The toggle's state. Binds two-way: <c>IsSelected="{Bind IsFavourite}"</c>.</summary>
        public Bindable<bool> IsSelected { get; } = new Bindable<bool>();

        private bool selected => IsToggle && IsSelected.Value;

        private void applyIcon()
        {
            icon.Symbol = selected && selectedSymbol != Symbol.None ? selectedSymbol : symbol;
            icon.Filled = selected;
        }

        protected override void OnClicked()
        {
            if (IsToggle && !IsSelected.Disabled)
                IsSelected.Value = !IsSelected.Value;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(size);

        protected override void UpdateColors(bool animate)
        {
            ColorRole? container;
            ColorRole content;
            ColorRole? outline = null;

            switch (variant)
            {
                case IconButtonVariant.Filled:
                    (container, content) = !IsToggle || selected ? (ColorRole.Primary, ColorRole.OnPrimary) : (ColorRole.SurfaceContainerHighest, ColorRole.Primary);
                    break;

                case IconButtonVariant.FilledTonal:
                    (container, content) = !IsToggle || selected
                        ? (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer)
                        : (ColorRole.SurfaceContainerHighest, ColorRole.OnSurfaceVariant);
                    break;

                case IconButtonVariant.Outlined:
                    if (selected)
                        (container, content) = (ColorRole.InverseSurface, ColorRole.InverseOnSurface);
                    else
                        (container, content, outline) = (null, ColorRole.OnSurfaceVariant, ColorRole.Outline);
                    break;

                default:
                    (container, content) = (null, selected ? ColorRole.Primary : ColorRole.OnSurfaceVariant);
                    break;
            }

            Colour4 containerColour = container == null ? Colour4.Transparent
                : IsEnabled ? Scheme[container.Value] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);

            Colour4? outlineColour = outline is ColorRole o ? IsEnabled ? Scheme[o] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer) : null;

            ApplyChrome(containerColour, Scheme[content], outlineColour, 0, animate);
            SetColour(icon, IsEnabled && iconColor is Colour4 c ? c : ContentColour(content), animate);
        }
    }

    /// <summary>The size of a <see cref="Fab"/>.</summary>
    public enum FabSize
    {
        /// <summary>40dp, 12dp corners.</summary>
        Small,

        /// <summary>56dp, 16dp corners: the default.</summary>
        Regular,

        /// <summary>96dp, 28dp corners and a 36dp icon.</summary>
        Large,
    }

    /// <summary>A floating action button's colours.</summary>
    public enum FabColor
    {
        /// <summary>Primary container, the default.</summary>
        Primary,

        /// <summary>Surface container high with a primary icon.</summary>
        Surface,

        Secondary,
        Tertiary,
    }

    /// <summary>
    /// The floating action button, for a screen's main action: <c>&lt;m3:Fab Icon="Edit" /&gt;</c>. It floats at elevation 3
    /// (1 when <see cref="Lowered"/>) and rises a level while hovered.
    /// </summary>
    public partial class Fab : ButtonBase
    {
        private readonly Icon icon;
        private FabSize fabSize = FabSize.Regular;
        private FabColor color;
        private bool lowered;

        public Fab()
        {
            ContentLayer.Add(icon = new Icon { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre });
            applySize();
        }

        public Symbol Icon
        {
            get => icon.Symbol;
            set => icon.Symbol = value;
        }

        public FabSize FabSize
        {
            get => fabSize;
            set
            {
                fabSize = value;
                applySize();
            }
        }

        public FabColor Color
        {
            get => color;
            set
            {
                color = value;
                OnStateChanged();
            }
        }

        /// <summary>Elevation 1 instead of 3, for FABs that sit on a surface rather than float over content.</summary>
        public bool Lowered
        {
            get => lowered;
            set
            {
                lowered = value;
                OnStateChanged();
            }
        }

        private void applySize()
        {
            (float side, ShapeCorner corner, float iconSize) = fabSize switch
            {
                FabSize.Small => (40f, ShapeCorner.Medium, 24f),
                FabSize.Large => (96f, ShapeCorner.ExtraLarge, 36f),
                _ => (56f, ShapeCorner.Large, 24f),
            };

            Size = new Vector2(side);
            Shape = corner;
            icon.Size = iconSize;
            InvalidateMeasure();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => Size;

        protected override void UpdateColors(bool animate)
        {
            var (container, content) = FabColors(color);
            int level = (lowered ? 1 : 3) + (IsHovered && !IsPressed ? 1 : 0);

            ApplyChrome(IsEnabled ? Scheme[container] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer), Scheme[content], null, level, animate);
            SetColour(icon, ContentColour(content), animate);
        }

        internal static (ColorRole Container, ColorRole Content) FabColors(FabColor color) => color switch
        {
            FabColor.Surface => (ColorRole.SurfaceContainerHigh, ColorRole.Primary),
            FabColor.Secondary => (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer),
            FabColor.Tertiary => (ColorRole.TertiaryContainer, ColorRole.OnTertiaryContainer),
            _ => (ColorRole.PrimaryContainer, ColorRole.OnPrimaryContainer),
        };
    }

    /// <summary>
    /// A FAB with a label: <c>&lt;m3:ExtendedFab Icon="Edit" Text="Compose" /&gt;</c>. Setting <see cref="Expanded"/> to false
    /// folds it into a regular FAB, as Material does while content scrolls.
    /// </summary>
    public partial class ExtendedFab : ButtonBase
    {
        private const float height = 56;
        private const float icon_size = 24;
        private const float gap = 12;

        private readonly Icon icon;
        private readonly Text label;
        private FabColor color;
        private bool lowered;
        private bool expanded = true;

        // How far the FAB is unfolded, 0 to 1; animated.
        private float expansion = 1;

        public ExtendedFab()
        {
            Height = height;
            Shape = ShapeCorner.Large;

            ContentLayer.AddRange(new Drawable[]
            {
                icon = new Icon { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
                label = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
            });
        }

        public Symbol Icon
        {
            get => icon.Symbol;
            set
            {
                icon.Symbol = value;
                icon.Alpha = value == Symbol.None ? 0 : 1;
                InvalidateMeasure();
            }
        }

        public LocalisableString Text
        {
            get => label.Text;
            set => label.Text = value;
        }

        public FabColor Color
        {
            get => color;
            set
            {
                color = value;
                OnStateChanged();
            }
        }

        public bool Lowered
        {
            get => lowered;
            set
            {
                lowered = value;
                OnStateChanged();
            }
        }

        /// <summary>Whether the label shows; false folds the button to a 56dp FAB (the icon is needed then).</summary>
        public bool Expanded
        {
            get => expanded;
            set
            {
                if (expanded == value)
                    return;

                expanded = value;

                if (IsLoaded)
                    this.TransformTo(nameof(Expansion), value ? 1f : 0f, Motion.Medium2, Motion.Emphasized);
                else
                    Expansion = value ? 1 : 0;
            }
        }

        /// <summary>The unfolding, animated by <see cref="Expanded"/>.</summary>
        public float Expansion
        {
            get => expansion;
            set
            {
                expansion = value;
                label.Alpha = Math.Clamp(value * 2 - 1, 0, 1);
                InvalidateMeasure();
            }
        }

        private bool hasIcon => icon.Symbol != Symbol.None;

        private float fullWidth => hasIcon ? 16 + icon_size + gap + label.DrawWidth + 20 : 20 + label.DrawWidth + 20;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
            => new Vector2(hasIcon ? height + (fullWidth - height) * expansion : fullWidth, height);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // Folded, the icon sits in the middle of the 56dp square; unfolded, 16dp from the start.
            icon.X = hasIcon ? (height - icon_size) / 2 + (16 - (height - icon_size) / 2) * expansion : 0;
            label.X = hasIcon ? 16 + icon_size + gap : 20;
        }

        protected override void UpdateColors(bool animate)
        {
            var (container, content) = Fab.FabColors(color);
            int level = (lowered ? 1 : 3) + (IsHovered && !IsPressed ? 1 : 0);

            ApplyChrome(IsEnabled ? Scheme[container] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer), Scheme[content], null, level, animate);
            SetColour(icon, ContentColour(content), animate);
            SetColour(label, ContentColour(content), animate);
        }
    }
}
