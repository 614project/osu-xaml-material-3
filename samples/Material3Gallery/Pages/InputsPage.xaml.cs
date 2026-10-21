using osu.Framework.Allocation;
using OsuXaml.Controls;

namespace Material3Gallery
{
    public partial class InputsPage : UserControl
    {
        private GalleryViewModel viewModel;
        private bool syncing;

        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel)
        {
            this.viewModel = viewModel;
            InitializeComponent(viewModel);

            // "All topics" is checked when every topic is, unchecked when none is, and indeterminate in between.
            var topics = new[] { newsBox, sportsBox, musicBox };

            foreach (var box in topics)
                box.IsChecked.BindValueChanged(_ => updateAll(topics));

            viewModel.AllTopics.BindValueChanged(e =>
            {
                if (syncing || e.NewValue == null)
                    return;

                syncing = true;
                foreach (var box in topics)
                    box.IsChecked.Value = e.NewValue;
                syncing = false;
            });

            updateAll(topics);
        }

        private void updateAll(OsuXaml.Material3.Checkbox[] topics)
        {
            if (syncing)
                return;

            int checkedCount = 0;
            foreach (var box in topics)
                if (box.IsChecked.Value == true)
                    checkedCount++;

            syncing = true;
            viewModel.AllTopics.Value = checkedCount == 0 ? false : checkedCount == topics.Length ? true : null;
            syncing = false;
        }
    }
}
