using osu.Framework;
using osu.Framework.Allocation;
using OsuXaml.Material3;

namespace Material3Gallery
{
    public partial class GalleryGame : Game
    {
        [Cached]
        private readonly GalleryViewModel viewModel = new GalleryViewModel();

        public int StartPage { get; set; }

        public bool StartDark { get; set; }

        public int StartSeed { get; set; } = -1;

        public int StartVariant { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            this.AddMaterial3();

            viewModel.Page.Value = StartPage;
            viewModel.IsDark.Value = StartDark;
            viewModel.Seed.Value = StartSeed;
            viewModel.Variant.Value = StartVariant;

            // osu-xaml's own controls (the scroll bars) take colours from the Material scheme too.
            MaterialTheme.Default.Scheme.Value.ApplyToOsuXamlTheme();

            Add(new MainWindow());
        }
    }
}
