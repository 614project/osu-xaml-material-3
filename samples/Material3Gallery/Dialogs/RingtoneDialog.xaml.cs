using osu.Framework.Allocation;
using OsuXaml.Material3;

namespace Material3Gallery
{
    public partial class RingtoneDialog : AlertDialog
    {
        [BackgroundDependencyLoader]
        private void load() => InitializeComponent();

        private void OnOk() => DialogResult = true;
    }
}
