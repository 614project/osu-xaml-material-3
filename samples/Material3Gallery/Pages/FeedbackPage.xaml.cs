using osu.Framework.Allocation;
using OsuXaml.Controls;
using OsuXaml.Material3;

namespace Material3Gallery
{
    public partial class FeedbackPage : UserControl
    {
        private GalleryViewModel viewModel;

        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel)
        {
            this.viewModel = viewModel;
            InitializeComponent(viewModel);

            // The sort menu keeps one item selected.
            sortMenu.ItemClicked += item =>
            {
                foreach (var d in sortMenu.Items)
                {
                    if (d is OsuXaml.Material3.MenuItem other)
                        other.IsSelected = other == item;
                }
            };
        }

        private void OnEditItem(OsuXaml.Material3.MenuItem item) => viewModel.Status.Value = item.Text.ToString();

        // AlertDialog.Show can't block the update thread, so it returns a task to await.
        private async void OnBasicDialog()
        {
            bool? result = await AlertDialog.Show(this, "Discard draft?", "Your message hasn't been sent and will be lost.", "Discard", "Keep editing");
            viewModel.Status.Value = result == true ? "Discarded" : "Kept";
        }

        private async void OnIconDialog()
        {
            bool? result = await AlertDialog.Show(this, "Permanently delete?",
                "Deleting the selected messages will also remove them from synced devices.", "Delete", "Cancel", Symbol.Delete);
            viewModel.Status.Value = result == true ? "Deleted" : "Cancelled";
        }

        private async void OnContentDialog()
        {
            bool? result = await new RingtoneDialog().ShowDialog(this);
            viewModel.Status.Value = result == true ? "Ringtone saved" : "Ringtone unchanged";
        }

        private void OnSnackbar() => SnackbarHost.Find(this)?.Show("Photo saved to the gallery");

        private void OnSnackbarAction()
            => SnackbarHost.Find(this)?.Show("Conversation archived", "Undo", () => viewModel.Status.Value = "Archive undone");

        private void OnSnackbarLong()
            => SnackbarHost.Find(this)?.Show("Can't send the photo while offline. It will go out as soon as the connection comes back.",
                "Retry", () => viewModel.Status.Value = "Retrying", showClose: true, duration: SnackbarDuration.Long);
    }
}
