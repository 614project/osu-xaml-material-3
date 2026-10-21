using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// A row of Material's list: an optional leading icon or element (an <see cref="Avatar"/>, an image), a headline with optional
    /// overline and supporting text, and an optional trailing icon, text or element (a checkbox, a switch).
    /// <code>
    /// &lt;m3:ListItem Headline="Ada Lovelace" SupportingText="Analytical engine notes" TrailingText="10 min"&gt;
    ///   &lt;m3:ListItem.Leading&gt;&lt;m3:Avatar Text="AL" /&gt;&lt;/m3:ListItem.Leading&gt;
    /// &lt;/m3:ListItem&gt;
    /// </code>
    /// One line is 56dp tall, two 72dp, three (an overline and supporting text, or supporting text that wraps) 88dp. A list item with
    /// a Click handler (or <see cref="IsClickable"/>) shows hover and press feedback.
    /// </summary>
    public partial class ListItem : MaterialControl
    {
        private const float padding = 16;
        private const float gap = 16;

        private readonly StateLayer stateLayer;
        private readonly Box selectedFill;
        private readonly Container leadingHolder;
        private readonly Container trailingHolder;
        private readonly Icon leadingIcon;
        private readonly Icon trailingIcon;
        private readonly Text overline;
        private readonly Text headline;
        private readonly Text supporting;
        private readonly Text trailingText;

        private Drawable leading;
        private Drawable trailing;
        private bool isClickable;
        private bool isSelected;

        public ListItem()
        {
            InternalChildren = new Drawable[]
            {
                selectedFill = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                stateLayer = new StateLayer(),
                leadingHolder = new Container { AutoSizeAxes = Axes.Both, X = padding },
                overline = new Text(TypeStyle.LabelSmall) { ColorRole = null, TextTrimming = TextTrimming.CharacterEllipsis },
                headline = new Text(TypeStyle.BodyLarge) { ColorRole = null, TextTrimming = TextTrimming.CharacterEllipsis },
                supporting = new Text(TypeStyle.BodyMedium) { ColorRole = null, TextWrapping = TextWrapping.Wrap },
                trailingHolder = new Container { AutoSizeAxes = Axes.Both, Anchor = Anchor.TopRight, Origin = Anchor.TopRight, X = -padding },
            };

            leadingIcon = new Icon { ColorRole = null };
            trailingIcon = new Icon { ColorRole = null };
            trailingText = new Text(TypeStyle.LabelSmall) { ColorRole = null };
        }

        public LocalisableString Headline
        {
            get => headline.Text;
            set
            {
                headline.Text = value;
                InvalidateMeasure();
            }
        }

        public LocalisableString SupportingText
        {
            get => supporting.Text;
            set
            {
                supporting.Text = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Small text above the headline.</summary>
        public LocalisableString Overline
        {
            get => overline.Text;
            set
            {
                overline.Text = value;
                InvalidateMeasure();
            }
        }

        /// <summary>A 24dp icon at the start.</summary>
        public Symbol LeadingIcon
        {
            get => leadingIcon.Symbol;
            set
            {
                leadingIcon.Symbol = value;
                Leading = value == Symbol.None ? null : leadingIcon;
            }
        }

        /// <summary>An element at the start: an <see cref="Avatar"/>, an image, a checkbox.</summary>
        public Drawable Leading
        {
            get => leading;
            set => setSlot(leadingHolder, ref leading, value);
        }

        /// <summary>A 24dp icon at the end.</summary>
        public Symbol TrailingIcon
        {
            get => trailingIcon.Symbol;
            set
            {
                trailingIcon.Symbol = value;
                Trailing = value == Symbol.None ? null : trailingIcon;
            }
        }

        /// <summary>Short text at the end, such as a time or count.</summary>
        public LocalisableString TrailingText
        {
            get => trailingText.Text;
            set
            {
                trailingText.Text = value;
                Trailing = string.IsNullOrEmpty(value.ToString()) ? null : trailingText;
            }
        }

        /// <summary>An element at the end: a checkbox, a switch, an icon button.</summary>
        public Drawable Trailing
        {
            get => trailing;
            set => setSlot(trailingHolder, ref trailing, value);
        }

        /// <summary>Raised when the item is clicked.</summary>
        public event Action Click;

        /// <summary>The same as <see cref="Click"/>, for bindings.</summary>
        public Action Action { get; set; }

        /// <summary>Whether the item reacts to the pointer. A Click handler sets it.</summary>
        public bool IsClickable
        {
            get => isClickable || Click != null || Action != null;
            set => isClickable = value;
        }

        /// <summary>Highlights the item in the secondary container colour, as for the open item of a list.</summary>
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                OnStateChanged();
            }
        }

        private void setSlot(Container holder, ref Drawable field, Drawable value)
        {
            if (field != null && field.Parent == holder)
                holder.Remove(field, field != leadingIcon && field != trailingIcon && field != trailingText);

            field = value;

            if (value != null)
                holder.Add(value);

            InvalidateMeasure();
        }

        private bool has(Text text) => !string.IsNullOrEmpty(text.Text.ToString());

        private float textLeft => leading != null ? padding + leadingHolder.DrawWidth + gap : padding;

        private float textRight(float width) => width - (trailing != null ? padding + trailingHolder.DrawWidth + gap : padding);

        // Lays the text out for a width and returns the item's height.
        private float layoutText(float width)
        {
            float textWidth = Math.Max(1, textRight(width) - textLeft);

            overline.Alpha = has(overline) ? 1 : 0;
            supporting.Alpha = has(supporting) ? 1 : 0;

            headline.Measure(new Vector2(textWidth, float.PositiveInfinity));
            if (has(overline)) overline.Measure(new Vector2(textWidth, float.PositiveInfinity));
            if (has(supporting)) supporting.Measure(new Vector2(textWidth, float.PositiveInfinity));

            bool supportingWraps = has(supporting) && supporting.DrawHeight > 24;
            int lines = 1 + (has(overline) ? 1 : 0) + (has(supporting) ? supportingWraps ? 2 : 1 : 0);

            return lines >= 3 ? 88 : lines == 2 ? 72 : 56;
        }

        private bool threeLine => DrawHeight >= 88;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            float width = float.IsFinite(availableSize.X) ? availableSize.X : 360;
            return new Vector2(Math.Min(width, 360), Math.Max(layoutText(width), leadingHolder.DrawHeight + 16));
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            layoutText(DrawWidth);

            float textHeight = (has(overline) ? overline.DrawHeight : 0) + headline.DrawHeight + (has(supporting) ? supporting.DrawHeight : 0);

            // Three-line items align to the top; shorter ones centre everything.
            float top = threeLine ? 12 : (DrawHeight - textHeight) / 2;
            float x = textLeft;

            overline.Position = new Vector2(x, top);
            headline.Position = new Vector2(x, top + (has(overline) ? overline.DrawHeight : 0));
            supporting.Position = new Vector2(x, headline.Y + headline.DrawHeight);

            leadingHolder.Y = threeLine ? 12 : (DrawHeight - leadingHolder.DrawHeight) / 2;
            trailingHolder.Y = threeLine ? 12 : (DrawHeight - trailingHolder.DrawHeight) / 2;
        }

        protected override bool OnHover(HoverEvent e)
        {
            base.OnHover(e);
            return IsClickable;
        }

        protected override void OnPressed(Vector2 screenSpacePosition)
        {
            if (IsClickable)
                stateLayer.Press(screenSpacePosition);
        }

        protected override void OnReleased() => stateLayer.Release();

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsClickable || !IsEnabled)
                return false;

            Click?.Invoke();
            Action?.Invoke();
            return true;
        }

        protected override void UpdateColors(bool animate)
        {
            bool selected = isSelected && IsEnabled;

            selectedFill.FadeTo(selected ? 1 : 0, animate ? Motion.Short4 : 0);
            SetColour(selectedFill, Scheme.SecondaryContainer, animate);

            SetColour(headline, ContentColour(selected ? ColorRole.OnSecondaryContainer : ColorRole.OnSurface), animate);
            SetColour(overline, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(supporting, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(trailingText, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(leadingIcon, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(trailingIcon, ContentColour(ColorRole.OnSurfaceVariant), animate);

            stateLayer.Color = Scheme.OnSurface;
            stateLayer.Hovered = IsHovered && IsClickable;
            stateLayer.Enabled = IsEnabled;
        }
    }

    /// <summary>
    /// A round avatar: initials (<see cref="Text"/>) or an icon on the primary container colour. 40dp by default.
    /// <c>&lt;m3:Avatar Text="JK" /&gt;</c>
    /// </summary>
    public partial class Avatar : MaterialControl
    {
        private readonly Circle fill;
        private readonly Text initials;
        private readonly Icon icon;
        private float diameter = 40;

        public Avatar()
        {
            Size = new Vector2(diameter);

            InternalChildren = new Drawable[]
            {
                fill = new Circle { RelativeSizeAxes = Axes.Both },
                initials = new Text(TypeStyle.TitleMedium) { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre },
                icon = new Icon(Symbol.Person) { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre },
            };
        }

        /// <summary>Initials to show; without them the avatar shows <see cref="Icon"/>.</summary>
        public LocalisableString Text
        {
            get => initials.Text;
            set
            {
                initials.Text = value;
                icon.Alpha = string.IsNullOrEmpty(value.ToString()) ? 1 : 0;
            }
        }

        /// <summary>The icon shown without initials; Person by default.</summary>
        public Symbol Icon
        {
            get => icon.Symbol;
            set => icon.Symbol = value;
        }

        public float Diameter
        {
            get => diameter;
            set
            {
                diameter = value;
                Size = new Vector2(value);
                icon.Size = value * 0.6f;
                InvalidateMeasure();
            }
        }

        /// <summary>The container colour; PrimaryContainer by default.</summary>
        public ColorRole Role { get; set; } = ColorRole.PrimaryContainer;

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(diameter);

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
            SetColour(fill, Scheme[Role], animate);
            SetColour(initials, Scheme[ColorScheme.ContentRoleFor(Role)], animate);
            SetColour(icon, Scheme[ColorScheme.ContentRoleFor(Role)], animate);
        }
    }
}
