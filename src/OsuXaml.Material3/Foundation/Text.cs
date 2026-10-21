using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using OsuXaml.Controls;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Text in a type scale role and a colour role: <c>&lt;m3:Text TypeStyle="HeadlineSmall" ColorRole="OnSurface" Text="Hello" /&gt;</c>.
    /// It is a <see cref="TextBlock"/>, so wrapping, trimming, alignment and inlines all work; its line height is the role's.
    /// </summary>
    public partial class Text : TextBlock
    {
        private TypeStyle typeStyle;
        private ColorRole? colorRole = Material3.ColorRole.OnSurface;

        [Resolved(canBeNull: true)]
        private MaterialTheme theme { get; set; }

        private IBindable<ColorScheme> scheme;

        public Text()
        {
            TypeStyle = TypeStyle.BodyLarge;
            Colour = MaterialTheme.Default.Scheme.Value.OnSurface;
        }

        public Text(TypeStyle style)
            : this()
        {
            TypeStyle = style;
        }

        public TypeStyle TypeStyle
        {
            get => typeStyle;
            set
            {
                typeStyle = value;
                Typescale.Apply(this, value);
                Layout.InvalidateMeasure(this);
            }
        }

        /// <summary>The scheme colour the text takes and follows (OnSurface by default); null leaves Colour to code.</summary>
        public ColorRole? ColorRole
        {
            get => colorRole;
            set
            {
                colorRole = value;
                applyColour(false);
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme = (theme ?? MaterialTheme.Default).Scheme.GetBoundCopy();
            scheme.BindValueChanged(_ => applyColour(IsLoaded), true);
        }

        private void applyColour(bool animate)
        {
            if (colorRole is Material3.ColorRole role && scheme?.Value is ColorScheme current)
                this.FadeColour(current[role], animate ? Motion.Medium2 : 0, Motion.Standard);
        }
    }
}
