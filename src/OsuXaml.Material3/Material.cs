using System.Runtime.CompilerServices;
using osu.Framework;
using osu.Framework.IO.Stores;

namespace OsuXaml.Material3
{
    /// <summary>Sets a game up for the Material 3 components.</summary>
    public static class Material
    {
        private static readonly ConditionalWeakTable<Game, object> set_up = new ConditionalWeakTable<Game, object>();

        /// <summary>
        /// Adds the fonts the components draw with (Roboto Medium and Material Symbols, outlined and filled) to the game's fonts.
        /// Call it from the game's <c>[BackgroundDependencyLoader]</c>, before adding any components. Calling it again does nothing.
        /// </summary>
        public static void AddMaterial3(this Game game)
        {
            lock (set_up)
            {
                if (set_up.TryGetValue(game, out _))
                    return;

                set_up.Add(game, null);
            }

            var resources = new ResourceStore<byte[]>(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Material).Assembly), "Resources"));

            game.AddFont(resources, @"Fonts/Roboto-Medium");
            game.AddFont(resources, @"Fonts/" + Icon.OutlinedFont);
            game.AddFont(resources, @"Fonts/" + Icon.FilledFont);
        }

        /// <summary>
        /// Sets osu-xaml's default control colours (<see cref="Controls.Theme"/>) from a Material scheme, so osu-xaml's own controls
        /// (scroll bars, combo boxes, data grids) sit well beside the Material ones. osu-xaml reads them as controls are created,
        /// so call it before building the UI (and again before rebuilding it after a theme change).
        /// </summary>
        public static void ApplyToOsuXamlTheme(this ColorScheme scheme)
        {
            Controls.Theme.Base = scheme.Surface;
            Controls.Theme.Mantle = scheme.SurfaceContainerLow;
            Controls.Theme.Crust = scheme.SurfaceContainerLowest;
            Controls.Theme.Text = scheme.OnSurface;
            Controls.Theme.SubtleText = scheme.OnSurfaceVariant;
            Controls.Theme.Surface = scheme.SurfaceContainerHighest;
            Controls.Theme.Hover = scheme.SurfaceContainerHigh;
            Controls.Theme.Border = scheme.OutlineVariant;
            Controls.Theme.Overlay = scheme.Outline;
            Controls.Theme.Accent = scheme.Primary;
            Controls.Theme.AccentText = scheme.OnPrimary;
            Controls.Theme.Danger = scheme.Error;
        }
    }
}
