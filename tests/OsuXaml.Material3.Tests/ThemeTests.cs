using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using OsuXaml.Controls;

namespace OsuXaml.Material3.Tests
{
    [HeadlessTest]
    public partial class ThemeTests : MaterialTestScene
    {
        [Test]
        public void ComponentsFollowTheAppTheme()
        {
            Text text = null;

            AddStep("create", () => Child = text = new Text { Text = "Primary", ColorRole = ColorRole.Primary });
            AddUntilStep("baseline primary", () => close(text.Colour, ColorScheme.Baseline(false).Primary));

            AddStep("go dark", () => MaterialTheme.Default.IsDark.Value = true);
            AddUntilStep("dark primary", () => close(text.Colour, ColorScheme.Baseline(true).Primary));

            AddStep("seed and variant", () =>
            {
                MaterialTheme.Default.SeedColor.Value = Colour4.FromHex("006E1C");
                MaterialTheme.Default.Variant.Value = SchemeVariant.Vibrant;
            });
            AddAssert("scheme made from them", () =>
            {
                var scheme = MaterialTheme.Default.Scheme.Value;
                return scheme.IsDark && scheme.Variant == SchemeVariant.Vibrant && scheme.Seed == Colour4.FromHex("006E1C");
            });
            AddUntilStep("seeded primary", () => close(text.Colour, ColorScheme.FromSeed(Colour4.FromHex("006E1C"), true, SchemeVariant.Vibrant).Primary));
        }

        [Test]
        public void ThemeScopeOverridesWhatItSets()
        {
            ThemeScope scope = null;
            Text inside = null;
            Text outside = null;

            AddStep("create", () =>
            {
                scope = new ThemeScope { SeedColor = Colour4.FromHex("B3261E") };
                scope.Content = inside = new Text { Text = "Inside", ColorRole = ColorRole.Primary };

                var panel = new StackPanel();
                panel.Add(outside = new Text { Text = "Outside", ColorRole = ColorRole.Primary });
                panel.Add(scope);
                Child = panel;
            });
            AddUntilStep("loaded", () => inside.IsLoaded);

            AddAssert("Of finds the scope", () => MaterialTheme.Of(inside) == scope.Theme && MaterialTheme.Of(outside) == MaterialTheme.Default);
            AddUntilStep("inside takes the seed", () => close(inside.Colour, ColorScheme.FromSeed(Colour4.FromHex("B3261E"), false).Primary));
            AddAssert("outside keeps the baseline", () => close(outside.Colour, ColorScheme.Baseline(false).Primary));
            AddAssert("scope paints its surface", () => close(scope.Background, ColorScheme.FromSeed(Colour4.FromHex("B3261E"), false).Surface));

            AddStep("app goes dark", () => MaterialTheme.Default.IsDark.Value = true);
            AddUntilStep("the scope follows what it doesn't set", () => scope.Theme.IsDark.Value
                                                                         && close(inside.Colour, ColorScheme.FromSeed(Colour4.FromHex("B3261E"), true).Primary));

            AddStep("scope stays light", () => scope.IsDark = false);
            AddUntilStep("light inside, dark outside", () => close(inside.Colour, ColorScheme.FromSeed(Colour4.FromHex("B3261E"), false).Primary)
                                                             && close(outside.Colour, ColorScheme.Baseline(true).Primary));
        }

        [Test]
        public void OsuXamlControlsTakeTheScheme()
        {
            var accent = Theme.Accent;
            var text = Theme.Text;

            AddStep("apply", () => ColorScheme.Baseline(true).ApplyToOsuXamlTheme());
            AddAssert("accent and text", () => Theme.Accent == ColorScheme.Baseline(true).Primary && Theme.Text == ColorScheme.Baseline(true).OnSurface);
            AddStep("restore", () =>
            {
                Theme.Accent = accent;
                Theme.Text = text;
            });
        }

        private static bool close(Colour4 a, Colour4 b) => System.Math.Abs(a.R - b.R) < 0.01f && System.Math.Abs(a.G - b.G) < 0.01f && System.Math.Abs(a.B - b.B) < 0.01f;
    }
}
