using osu.Framework.Allocation;
using OsuXaml.Controls;
using OsuXaml.Material3;

namespace Material3Gallery
{
    public partial class ContainersPage : UserControl
    {
        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel) => InitializeComponent(viewModel);

        private void OnCardClick() => SnackbarHost.Find(this)?.Show("Card clicked");

        private void OnItemClick() => SnackbarHost.Find(this)?.Show("List item clicked");
    }
}
