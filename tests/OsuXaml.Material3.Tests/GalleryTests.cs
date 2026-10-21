using System.Linq;
using Material3Gallery;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using OsuXaml.Controls;

namespace OsuXaml.Material3.Tests
{
    /// <summary>Loads the sample gallery and visits every page in each brightness and a few seeds, so the samples keep working.</summary>
    [HeadlessTest]
    public partial class GalleryTests : MaterialTestScene
    {
        [Cached]
        private readonly GalleryViewModel viewModel = new GalleryViewModel();

        [Test]
        public void EveryPageLoads()
        {
            AddStep("create window", () => Child = new MainWindow());
            AddUntilStep("loaded", () => this.ChildrenOfType<MainWindow>().SingleOrDefault()?.IsLoaded == true);

            for (int i = 0; i < GalleryViewModel.PageTitles.Length; i++)
            {
                int page = i;
                AddStep($"page {page}", () => viewModel.Page.Value = page);
                AddUntilStep("shown", () => visiblePages().SequenceEqual(new[] { page }));
                AddAssert("title", () => this.ChildrenOfType<TopAppBar>().First().Title.ToString() == GalleryViewModel.PageTitles[page]);
            }

            AddStep("dark, green, expressive", () =>
            {
                viewModel.IsDark.Value = true;
                viewModel.Seed.Value = 2;
                viewModel.Variant.Value = (int)SchemeVariant.Expressive;
            });
            AddAssert("theme follows", () => MaterialTheme.Default.IsDark.Value && MaterialTheme.Default.SeedColor.Value == GalleryViewModel.Seeds[2]
                                             && MaterialTheme.Default.Variant.Value == SchemeVariant.Expressive);
            AddAssert("seed choice follows", () => viewModel.SeedChoice.Value == 3);

            AddStep("back to the baseline", () => viewModel.SeedChoice.Value = 0);
            AddAssert("baseline", () => viewModel.Seed.Value == -1 && MaterialTheme.Default.SeedColor.Value == null);
            AddStep("light, tonal spot", () =>
            {
                viewModel.IsDark.Value = false;
                viewModel.Variant.Value = 0;
            });
        }

        private int[] visiblePages()
        {
            var pages = new Drawable[]
            {
                this.ChildrenOfType<ButtonsPage>().Single(), this.ChildrenOfType<InputsPage>().Single(), this.ChildrenOfType<ContainersPage>().Single(),
                this.ChildrenOfType<NavigationPage>().Single(), this.ChildrenOfType<FeedbackPage>().Single(), this.ChildrenOfType<ThemePage>().Single(),
            };

            return Enumerable.Range(0, pages.Length).Where(i => !Layout.IsCollapsed(pages[i]) && pages[i].IsPresent).ToArray();
        }
    }
}
