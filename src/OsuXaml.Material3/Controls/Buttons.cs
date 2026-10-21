using System;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's common buttons: a 40dp pill with a label and an optional 18dp icon. Use one of <see cref="FilledButton"/>,
    /// <see cref="FilledTonalButton"/>, <see cref="ElevatedButton"/>, <see cref="OutlinedButton"/> or <see cref="TextButton"/>.
    /// <c>&lt;m3:FilledButton Text="Save" Icon="Check" Click="{Bind Save}" /&gt;</c>
    /// </summary>
    public abstract partial class CommonButton : ButtonBase
    {
        private const float height = 40;
        private const float icon_size = 18;
        private const float gap = 8;

        private readonly Text label;
        private readonly Icon icon;
        private bool trailingIcon;

        protected CommonButton()
        {
            Height = height;

            ContentLayer.AddRange(new Drawable[]
            {
                icon = new Icon { Size = icon_size, ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
                label = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
            });
        }

        public LocalisableString Text
        {
            get => label.Text;
            set => label.Text = value;
        }

        /// <summary>An icon beside the label; None (the default) for none.</summary>
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

        /// <summary>Puts the icon after the label.</summary>
        public bool TrailingIcon
        {
            get => trailingIcon;
            set
            {
                trailingIcon = value;
                InvalidateMeasure();
            }
        }

        private bool hasIcon => icon.Symbol != Symbol.None;

        /// <summary>Space on the icon's side, and on the other side.</summary>
        protected virtual (float IconSide, float OtherSide) ContentPadding => hasIcon ? (16, 24) : (24, 24);

        private float contentWidth => label.DrawWidth + (hasIcon ? icon_size + gap : 0);

        private (float Left, float Right) padding
        {
            get
            {
                var p = ContentPadding;
                return hasIcon && trailingIcon ? (p.OtherSide, p.IconSide) : (p.IconSide, p.OtherSide);
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var p = padding;
            return new Vector2(p.Left + contentWidth + p.Right, height);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // The content keeps its padding and centres in whatever extra width a stretched button has.
            var p = padding;
            float x = p.Left + Math.Max(0, (DrawWidth - (p.Left + contentWidth + p.Right)) / 2);

            if (hasIcon && trailingIcon)
            {
                label.X = x;
                icon.X = x + label.DrawWidth + gap;
            }
            else if (hasIcon)
            {
                icon.X = x;
                label.X = x + icon_size + gap;
            }
            else
                label.X = x;
        }

        /// <summary>The container role (null for none), content role, outline role (null for none) and resting/hovered elevation.</summary>
        protected abstract (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style { get; }

        protected override void UpdateColors(bool animate)
        {
            var style = Style;
            var content = ContentColour(style.Content);

            Colour4 container = !IsEnabled
                ? style.Container != null ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer) : Colour4.Transparent
                : style.Container is ColorRole c ? Scheme[c] : Colour4.Transparent;

            Colour4? outline = style.Outline is ColorRole o ? IsEnabled ? Scheme[o] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer) : null;

            ApplyChrome(container, Scheme[style.Content], outline, IsHovered && !IsPressed ? style.HoverLevel : style.Level, animate);
            SetColour(label, content, animate);
            SetColour(icon, content, animate);
        }
    }

    /// <summary>The high-emphasis button, for the main action: a primary container.</summary>
    public partial class FilledButton : CommonButton
    {
        protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
            => (ColorRole.Primary, ColorRole.OnPrimary, null, 0, 1);
    }

    /// <summary>A medium-emphasis button in the secondary container colour.</summary>
    public partial class FilledTonalButton : CommonButton
    {
        protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
            => (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer, null, 0, 1);
    }

    /// <summary>A filled tonal button lifted with a shadow, for buttons over patterned or image backgrounds.</summary>
    public partial class ElevatedButton : CommonButton
    {
        protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
            => (ColorRole.SurfaceContainerLow, ColorRole.Primary, null, 1, 2);
    }

    /// <summary>A medium-emphasis button with an outline and no container, for secondary actions.</summary>
    public partial class OutlinedButton : CommonButton
    {
        protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
            => (null, ColorRole.Primary, ColorRole.Outline, 0, 0);
    }

    /// <summary>The lowest-emphasis button: a label in the primary colour, for dialogs and cards.</summary>
    public partial class TextButton : CommonButton
    {
        protected override (float IconSide, float OtherSide) ContentPadding => Icon != Symbol.None ? (12, 16) : (12, 12);

        protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
            => (null, ColorRole.Primary, null, 0, 0);
    }
}
