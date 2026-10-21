using osu.Framework.Allocation;
using OsuXaml.Controls;

namespace Material3Gallery
{
    public partial class NavigationPage : UserControl
    {
        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel) => InitializeComponent(viewModel);
    }
}
