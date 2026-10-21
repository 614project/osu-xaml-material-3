using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// A Material Symbols icon: <c>&lt;m3:Icon Symbol="Favorite" Filled="True" ColorRole="Primary" /&gt;</c>. Icons are drawn in their
    /// whole 24dp grid square, padding included, so they line up as Material lays them out. 24 pixels by default.
    /// Needs <see cref="Material.AddMaterial3"/> to have added the fonts.
    /// </summary>
    public partial class Icon : SpriteIcon
    {
        internal const string Family = "MaterialSymbols";
        internal const string OutlinedFont = Family + "-Outlined";
        internal const string FilledFont = Family + "-Filled";

        private Symbol symbol;
        private bool filled;
        private ColorRole? colorRole = Material3.ColorRole.OnSurfaceVariant;

        [Resolved(canBeNull: true)]
        private MaterialTheme theme { get; set; }

        private IBindable<ColorScheme> scheme;

        public Icon()
        {
            Size = 24;
            Colour = MaterialTheme.Default.Scheme.Value.OnSurfaceVariant;

            // In a panel an icon starts its row, as text does, instead of centring itself as sized elements do.
            Controls.Layout.SetHorizontalAlignment(this, Controls.HorizontalAlignment.Left);
        }

        public Icon(Symbol symbol)
            : this()
        {
            Symbol = symbol;
        }

        /// <summary>Which icon.</summary>
        public Symbol Symbol
        {
            get => symbol;
            set
            {
                symbol = value;
                apply();
            }
        }

        /// <summary>The filled style (Material uses it for selected states); outlined by default.</summary>
        public bool Filled
        {
            get => filled;
            set
            {
                filled = value;
                apply();
            }
        }

        /// <summary>The scheme colour the icon takes and follows (OnSurfaceVariant by default); null leaves Colour to code.</summary>
        public ColorRole? ColorRole
        {
            get => colorRole;
            set
            {
                colorRole = value;
                applyColour(false);
            }
        }

        /// <summary>Width and height in pixels.</summary>
        public new float Size
        {
            get => base.Size.X;
            set => base.Size = new Vector2(value);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme = (theme ?? MaterialTheme.Default).Scheme.GetBoundCopy();
            scheme.BindValueChanged(_ => applyColour(IsLoaded), true);
        }

        private void apply() => base.Icon = new IconUsage((char)symbol, Family, filled ? "Filled" : "Outlined");

        private void applyColour(bool animate)
        {
            if (colorRole is Material3.ColorRole role && scheme?.Value is ColorScheme current)
                this.FadeColour(current[role], animate ? Motion.Medium2 : 0, Motion.Standard);
        }
    }
}
