using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;

namespace OsuXaml.Material3.Tests
{
    /// <summary>A test scene with Material's fonts added and the app-wide theme back at the light baseline before each test.</summary>
    public abstract partial class MaterialTestScene : ManualInputManagerTestScene
    {
        [Resolved]
        private Game game { get; set; }

        [BackgroundDependencyLoader]
        private void load() => game.AddMaterial3();

        [SetUpSteps]
        public virtual void SetUpSteps()
        {
            AddStep("reset theme", () =>
            {
                MaterialTheme.Default.IsDark.Value = false;
                MaterialTheme.Default.SeedColor.Value = null;
                MaterialTheme.Default.Variant.Value = SchemeVariant.TonalSpot;
            });
        }
    }
}
