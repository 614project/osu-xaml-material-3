using osu.Framework.Allocation;
using OsuXaml.Controls;

namespace Material3Gallery
{
    public partial class ButtonsPage : UserControl
    {
        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel) => InitializeComponent(viewModel);

        private void OnRemoveAda() => Layout.SetVisibility(adaChip, Visibility.Collapsed);

        private void OnRemoveAlan() => Layout.SetVisibility(alanChip, Visibility.Collapsed);
    }
}
