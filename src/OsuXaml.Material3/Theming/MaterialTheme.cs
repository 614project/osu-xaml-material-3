using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using OsuXaml.Controls;

namespace OsuXaml.Material3
{
    /// <summary>
    /// The colour scheme components use, made from <see cref="IsDark"/>, <see cref="SeedColor"/> and <see cref="Variant"/>.
    /// <see cref="Default"/> styles everything unless a <see cref="ThemeScope"/> (or a cached MaterialTheme) gives part of the
    /// tree its own. Changing any of them recolours every component, with a short cross-fade.
    /// </summary>
    public sealed class MaterialTheme
    {
        /// <summary>The app-wide theme.</summary>
        public static MaterialTheme Default { get; } = new MaterialTheme();

        private readonly Bindable<ColorScheme> scheme = new Bindable<ColorScheme>(ColorScheme.Baseline(false));

        public MaterialTheme()
        {
            IsDark.BindValueChanged(_ => update());
            SeedColor.BindValueChanged(_ => update());
            Variant.BindValueChanged(_ => update());
        }

        /// <summary>Dark theme.</summary>
        public Bindable<bool> IsDark { get; } = new Bindable<bool>();

        /// <summary>The colour dynamic colour derives the scheme from; null (the default) uses Material's baseline scheme.</summary>
        public Bindable<Colour4?> SeedColor { get; } = new Bindable<Colour4?>();

        /// <summary>How a seed colour spreads into palettes.</summary>
        public Bindable<SchemeVariant> Variant { get; } = new Bindable<SchemeVariant>();

        /// <summary>The current scheme. Components bind to it; set the inputs above to change it.</summary>
        public IBindable<ColorScheme> Scheme => scheme;

        /// <summary>A theme that follows this one, where the child's own settings can then be changed.</summary>
        public MaterialTheme CreateChild()
        {
            var child = new MaterialTheme();
            child.IsDark.Value = IsDark.Value;
            child.SeedColor.Value = SeedColor.Value;
            child.Variant.Value = Variant.Value;
            return child;
        }

        private void update()
            => scheme.Value = SeedColor.Value is Colour4 seed ? ColorScheme.FromSeed(seed, IsDark.Value, Variant.Value) : ColorScheme.Baseline(IsDark.Value);

        /// <summary>The theme a drawable's dependencies provide, else <see cref="Default"/>.</summary>
        internal static MaterialTheme From(IReadOnlyDependencyContainer dependencies) => dependencies.Get<MaterialTheme>() ?? Default;

        /// <summary>
        /// The theme around a drawable: that of the nearest <see cref="ThemeScope"/> it is in, else <see cref="Default"/>. Popups,
        /// dialogs and tooltips open outside the scope of the element they belong to and use this to match it.
        /// </summary>
        public static MaterialTheme Of(Drawable drawable)
        {
            for (var d = drawable; d != null; d = d.Parent)
            {
                if (d is ThemeScope scope)
                    return scope.Theme;

                if (d is ThemedContainer themed && themed.Theme != null)
                    return themed.Theme;
            }

            return Default;
        }
    }

    /// <summary>A container that gives its content a theme, for content shown away from where it was declared (a popup's).</summary>
    internal partial class ThemedContainer : Container
    {
        /// <summary>The theme to give; set before the container loads.</summary>
        public MaterialTheme Theme { get; set; }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(parent);

            if (Theme != null)
                dependencies.Cache(Theme);

            return dependencies;
        }
    }

    /// <summary>
    /// Gives its content a theme of its own: <c>&lt;m3:ThemeScope IsDark="True"&gt;</c> shows a dark part in a light app.
    /// Settings left unset follow the theme around the scope. The scope paints its area in the theme's surface colour.
    /// </summary>
    public partial class ThemeScope : ContentControl
    {
        private readonly MaterialTheme theme = new MaterialTheme();
        private IBindable<ColorScheme> scheme;

        private bool? isDark;
        private Colour4? seedColor;
        private SchemeVariant? variant;
        private MaterialTheme outer;

        // Bound copies of the outer theme's settings, so the scope follows changes to the ones it doesn't set.
        private readonly Bindable<bool> outerDark = new Bindable<bool>();
        private readonly Bindable<Colour4?> outerSeed = new Bindable<Colour4?>();
        private readonly Bindable<SchemeVariant> outerVariant = new Bindable<SchemeVariant>();

        /// <summary>Dark or light for the content; unset follows the theme around the scope.</summary>
        public bool? IsDark
        {
            get => isDark;
            set
            {
                isDark = value;
                apply();
            }
        }

        public Colour4? SeedColor
        {
            get => seedColor;
            set
            {
                seedColor = value;
                apply();
            }
        }

        public SchemeVariant? Variant
        {
            get => variant;
            set
            {
                variant = value;
                apply();
            }
        }

        /// <summary>The role the scope paints behind its content; Surface by default, null for none.</summary>
        public ColorRole? BackgroundRole { get; set; } = ColorRole.Surface;

        public MaterialTheme Theme => theme;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            outer = MaterialTheme.From(parent);

            var dependencies = new DependencyContainer(parent);
            dependencies.Cache(theme);
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            outerDark.BindTo(outer.IsDark);
            outerSeed.BindTo(outer.SeedColor);
            outerVariant.BindTo(outer.Variant);

            outerDark.BindValueChanged(_ => apply());
            outerSeed.BindValueChanged(_ => apply());
            outerVariant.BindValueChanged(_ => apply(), true);

            scheme = theme.Scheme.GetBoundCopy();
            scheme.BindValueChanged(e =>
            {
                if (BackgroundRole is ColorRole role)
                {
                    BackgroundBox.Alpha = 1;
                    BackgroundBox.FadeColour(e.NewValue[role], IsLoaded ? Motion.Medium2 : 0, Motion.Standard);
                }

                Foreground = e.NewValue.OnSurface;
            }, true);
        }

        private void apply()
        {
            if (outer == null)
                return;

            theme.IsDark.Value = isDark ?? outerDark.Value;
            theme.SeedColor.Value = seedColor ?? outerSeed.Value;
            theme.Variant.Value = variant ?? outerVariant.Value;
        }
    }
}
