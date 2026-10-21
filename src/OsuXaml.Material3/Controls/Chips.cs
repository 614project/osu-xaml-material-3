using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What Material's chips share: a 32dp tall, 8dp rounded container with a label, an optional 18dp icon at either end, and an
    /// outline (or with <see cref="Elevated"/>, a tinted container with a shadow). Use <see cref="AssistChip"/>, <see cref="FilterChip"/>,
    /// <see cref="InputChip"/> or <see cref="SuggestionChip"/>.
    /// </summary>
    public abstract partial class Chip : ButtonBase
    {
        private const float height = 32;
        private const float icon_size = 18;
        private const float gap = 8;

        private readonly Text label;
        private readonly Icon leading;
        private readonly Icon trailing;
        private bool elevated;

        protected Chip()
        {
            Height = height;
            Shape = ShapeCorner.Small;

            ContentLayer.AddRange(new Drawable[]
            {
                leading = new Icon { Size = icon_size, ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
                label = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                trailing = new Icon { Size = icon_size, ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
            });
        }

        public LocalisableString Text
        {
            get => label.Text;
            set
            {
                label.Text = value;
                InvalidateMeasure();
            }
        }

        /// <summary>An icon before the label.</summary>
        public Symbol Icon
        {
            get => icon;
            set
            {
                icon = value;
                RefreshIcons();
            }
        }

        private Symbol icon;

        /// <summary>A tinted container with a shadow instead of an outline, for chips over images or busy backgrounds.</summary>
        public bool Elevated
        {
            get => elevated;
            set
            {
                elevated = value;
                OnStateChanged();
            }
        }

        /// <summary>The leading icon shown: the <see cref="Icon"/> by default; a filter chip shows a check while selected.</summary>
        protected virtual Symbol ShownLeadingIcon => icon;

        /// <summary>The trailing icon shown, if any.</summary>
        protected virtual Symbol ShownTrailingIcon => Symbol.None;

        /// <summary>Whether the leading icon is filled (as for selected chips).</summary>
        protected virtual bool LeadingFilled => false;

        protected Icon LeadingIconDrawable => leading;

        protected Icon TrailingIconDrawable => trailing;

        protected void RefreshIcons()
        {
            var shownLeading = ShownLeadingIcon;
            var shownTrailing = ShownTrailingIcon;

            leading.Alpha = shownLeading == Symbol.None ? 0 : 1;
            leading.Filled = LeadingFilled;
            leading.Symbol = shownLeading;

            trailing.Alpha = shownTrailing == Symbol.None ? 0 : 1;
            trailing.Symbol = shownTrailing;

            InvalidateMeasure();
        }

        private bool hasLeading => leading.Alpha > 0;

        private bool hasTrailing => trailing.Alpha > 0;

        private (float Left, float Right) padding => (hasLeading ? 8 : 16, hasTrailing ? 8 : 16);

        private float contentWidth => label.DrawWidth + (hasLeading ? icon_size + gap : 0) + (hasTrailing ? icon_size + gap : 0);

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var p = padding;
            return new Vector2(p.Left + contentWidth + p.Right, height);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            var p = padding;
            float x = p.Left + Math.Max(0, (DrawWidth - (p.Left + contentWidth + p.Right)) / 2);

            if (hasLeading)
            {
                leading.X = x;
                x += icon_size + gap;
            }

            label.X = x;
            x += label.DrawWidth + gap;
            trailing.X = x;
        }

        /// <summary>The container role (null for none), label role, leading icon role, and whether the chip shows as selected.</summary>
        protected abstract (ColorRole? Container, ColorRole Label, ColorRole Icon, bool Selected) Style { get; }

        protected override void UpdateColors(bool animate)
        {
            var style = Style;

            // A selected or elevated chip has a container; the rest are outlined.
            ColorRole? container = style.Container ?? (elevated ? ColorRole.SurfaceContainerLow : null);
            bool outlined = container == null;

            Colour4 containerColour = container == null ? Colour4.Transparent
                : IsEnabled ? Scheme[container.Value] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);

            Colour4? outline = outlined ? IsEnabled ? Scheme.OutlineVariant : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer) : null;

            int level = elevated && IsEnabled ? IsHovered && !IsPressed ? 2 : 1 : 0;

            ApplyChrome(containerColour, Scheme[style.Label], outline, level, animate);
            SetColour(label, ContentColour(style.Label), animate);
            SetColour(leading, ContentColour(style.Icon), animate);
            SetColour(trailing, ContentColour(style.Label == ColorRole.OnSecondaryContainer ? ColorRole.OnSecondaryContainer : ColorRole.OnSurfaceVariant), animate);
        }
    }

    /// <summary>
    /// A chip for a smart or automated action that leads somewhere, like "Add to calendar":
    /// <c>&lt;m3:AssistChip Text="Directions" Icon="Directions" /&gt;</c>.
    /// </summary>
    public partial class AssistChip : Chip
    {
        protected override (ColorRole? Container, ColorRole Label, ColorRole Icon, bool Selected) Style
            => (null, ColorRole.OnSurface, ColorRole.Primary, false);
    }

    /// <summary>
    /// A chip that narrows content, toggled on click: <c>&lt;m3:FilterChip Text="Vegan" IsSelected="{Bind Vegan}" /&gt;</c>. A selected
    /// chip fills with the secondary container and shows a check in place of its icon.
    /// </summary>
    public partial class FilterChip : Chip
    {
        public FilterChip()
        {
            IsSelected.BindValueChanged(_ =>
            {
                RefreshIcons();
                OnStateChanged();
            });
        }

        /// <summary>Whether the filter is on. Binds two-way.</summary>
        public Bindable<bool> IsSelected { get; } = new Bindable<bool>();

        /// <summary>A dropdown arrow after the label, for a filter that opens a menu of choices.</summary>
        public bool ShowDropdownIcon { get; set; }

        protected override Symbol ShownLeadingIcon => IsSelected.Value ? Symbol.Check : base.ShownLeadingIcon;

        protected override Symbol ShownTrailingIcon => ShowDropdownIcon ? Symbol.ArrowDropDown : Symbol.None;

        protected override void OnClicked()
        {
            if (!IsSelected.Disabled)
                IsSelected.Value = !IsSelected.Value;
        }

        protected override (ColorRole? Container, ColorRole Label, ColorRole Icon, bool Selected) Style
            => IsSelected.Value
                ? (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer, ColorRole.OnSecondaryContainer, true)
                : (null, ColorRole.OnSurfaceVariant, ColorRole.Primary, false);
    }

    /// <summary>
    /// A chip for something the user entered, such as a recipient, with a remove button:
    /// <c>&lt;m3:InputChip Text="Ada Lovelace" Removed="{Bind RemoveAda}" /&gt;</c>. Clicking the cross raises <see cref="Removed"/>;
    /// clicking the chip raises Click.
    /// </summary>
    public partial class InputChip : Chip
    {
        private bool removable = true;

        public InputChip()
        {
            IsSelected.BindValueChanged(_ => OnStateChanged());
        }

        /// <summary>Selected chips fill with the secondary container.</summary>
        public Bindable<bool> IsSelected { get; } = new Bindable<bool>();

        /// <summary>Whether the chip shows its remove cross (true by default).</summary>
        public bool IsRemovable
        {
            get => removable;
            set
            {
                removable = value;
                RefreshIcons();
            }
        }

        /// <summary>Raised when the remove cross is clicked.</summary>
        public event Action Removed;

        /// <summary>The same as <see cref="Removed"/>, for bindings.</summary>
        public Action RemoveAction { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            RefreshIcons();
        }

        protected override Symbol ShownTrailingIcon => removable ? Symbol.Close : Symbol.None;

        protected override bool OnClick(ClickEvent e)
        {
            if (IsEnabled && removable && TrailingIconDrawable.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
            {
                Removed?.Invoke();
                RemoveAction?.Invoke();
                return true;
            }

            return base.OnClick(e);
        }

        protected override (ColorRole? Container, ColorRole Label, ColorRole Icon, bool Selected) Style
            => IsSelected.Value
                ? (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer, ColorRole.Primary, true)
                : (null, ColorRole.OnSurfaceVariant, ColorRole.OnSurfaceVariant, false);
    }

    /// <summary>
    /// A chip offering a suggested response or search, like "Sounds good": <c>&lt;m3:SuggestionChip Text="Yes" /&gt;</c>.
    /// </summary>
    public partial class SuggestionChip : Chip
    {
        protected override (ColorRole? Container, ColorRole Label, ColorRole Icon, bool Selected) Style
            => (null, ColorRole.OnSurfaceVariant, ColorRole.Primary, false);
    }
}
