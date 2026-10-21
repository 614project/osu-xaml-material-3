using osu.Framework.Allocation;
using osu.Framework.Graphics;
using OsuXaml.Controls;

namespace Material3Gallery
{
    public partial class MainWindow : Window
    {
        private GalleryViewModel viewModel;

        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel)
        {
            this.viewModel = viewModel;
            InitializeComponent(viewModel);

            var pages = new Drawable[] { buttonsPage, inputsPage, containersPage, navigationPage, feedbackPage, themePage };

            viewModel.Page.BindValueChanged(e =>
            {
                for (int i = 0; i < pages.Length; i++)
                    Layout.SetVisibility(pages[i], i == e.NewValue ? Visibility.Visible : Visibility.Collapsed);

                scroller.ScrollToTop(false);
            }, true);

            // The app bar takes the scrolled colour once content passes under it.
            scroller.ScrollChanged += () => appBar.IsScrolled = scroller.VerticalOffset > 0;

            // Menu items: Baseline, a divider, then the seed colours in order.
            seedMenu.ItemClicked += item =>
            {
                int index = 0;

                foreach (var d in seedMenu.Items)
                {
                    if (d == item)
                        break;

                    if (d is OsuXaml.Material3.MenuItem)
                        index++;
                }

                viewModel.Seed.Value = index - 1;
            };

            variantMenu.ItemClicked += item =>
            {
                int index = 0;

                foreach (var d in variantMenu.Items)
                {
                    if (d == item)
                        break;

                    index++;
                }

                viewModel.Variant.Value = index;
            };
        }
    }
}
