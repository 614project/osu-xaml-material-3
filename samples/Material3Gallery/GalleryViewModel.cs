using osu.Framework.Bindables;
using osu.Framework.Graphics;
using OsuXaml.Material3;

namespace Material3Gallery
{
    public class GalleryViewModel
    {
        public static readonly string[] PageTitles = { "Buttons", "Inputs", "Containers", "Navigation", "Feedback", "Theme" };

        public GalleryViewModel()
        {
            Page.BindValueChanged(e => PageTitle.Value = PageTitles[e.NewValue], true);

            // The gallery's light/dark, seed colour and variant drive the app-wide theme.
            IsDark.BindValueChanged(e => MaterialTheme.Default.IsDark.Value = e.NewValue);
            Variant.BindValueChanged(e => MaterialTheme.Default.Variant.Value = (SchemeVariant)e.NewValue);
            Seed.BindValueChanged(e =>
            {
                MaterialTheme.Default.SeedColor.Value = e.NewValue < 0 ? null : Seeds[e.NewValue];
                SeedChoice.Value = e.NewValue + 1;
            });
            SeedChoice.BindValueChanged(e => Seed.Value = e.NewValue - 1);
        }

        public Bindable<int> Page { get; } = new Bindable<int>();

        public Bindable<string> PageTitle { get; } = new Bindable<string>();

        public Bindable<bool> IsDark { get; } = new Bindable<bool>();

        /// <summary>-1 for Material's baseline scheme, else an index into <see cref="Seeds"/>.</summary>
        public Bindable<int> Seed { get; } = new Bindable<int>(-1);

        /// <summary><see cref="Seed"/> plus one, for a list that starts with the baseline.</summary>
        public Bindable<int> SeedChoice { get; } = new Bindable<int>();

        public static readonly Colour4[] Seeds =
        {
            Colour4.FromHex("6750A4"),
            Colour4.FromHex("0061A4"),
            Colour4.FromHex("006E1C"),
            Colour4.FromHex("B3261E"),
            Colour4.FromHex("8B5000"),
            Colour4.FromHex("006A6A"),
        };

        public Bindable<int> Variant { get; } = new Bindable<int>();

        public void ToggleDark() => IsDark.Value = !IsDark.Value;

        // Buttons page
        public Bindable<int> Clicks { get; } = new Bindable<int>();

        public void Click() => Clicks.Value++;

        public Bindable<int> Period { get; } = new Bindable<int>(1);

        public Bindable<bool> FabExpanded { get; } = new Bindable<bool>(true);

        public void ToggleFab() => FabExpanded.Value = !FabExpanded.Value;

        // Inputs page
        public Bindable<string> Name { get; } = new Bindable<string>(string.Empty);

        public Bindable<string> Email { get; } = new Bindable<string>("ada@example.com");

        public BindableDouble Volume { get; } = new BindableDouble(40);

        public BindableDouble Brightness { get; } = new BindableDouble(60);

        public Bindable<bool> Notifications { get; } = new Bindable<bool>(true);

        public Bindable<bool?> AllTopics { get; } = new Bindable<bool?>(null);

        public Bindable<int> Size { get; } = new Bindable<int>(1);

        public void ClearEmail() => Email.Value = string.Empty;

        // Containers page
        public Bindable<int> Tab { get; } = new Bindable<int>();

        // Navigation page
        public Bindable<int> BarPage { get; } = new Bindable<int>();

        public Bindable<int> DrawerPage { get; } = new Bindable<int>();

        // Feedback page
        public BindableDouble Progress { get; } = new BindableDouble(0.6);

        public Bindable<string> Status { get; } = new Bindable<string>("Nothing yet");
    }
}
