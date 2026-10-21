using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using OsuXaml.Controls;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3.Tests
{
    /// <summary>Menus, dropdowns, dialogs, snackbars and tooltips, which open over a window.</summary>
    [HeadlessTest]
    public partial class PopupTests : MaterialTestScene
    {
        private Window window;

        private void create(System.Func<Drawable> content)
        {
            AddStep("create window", () =>
            {
                var panel = new StackPanel { Padding = new MarginPadding(20), Spacing = 16 };
                panel.Add(content());
                Child = window = new Window { Child = panel };
            });
        }

        private void click(System.Func<Drawable> target) => AddStep("click", () =>
        {
            InputManager.MoveMouseTo(target());
            InputManager.Click(MouseButton.Left);
        });

        [Test]
        public void ButtonMenuOpensAndPicks()
        {
            FilledButton button = null;
            Menu menu = null;
            MenuItem cut = null;
            MenuItem paste = null;
            MenuItem picked = null;
            int actions = 0;

            create(() =>
            {
                picked = null;
                actions = 0;

                menu = new Menu();
                menu.Add(cut = new MenuItem { Text = "Cut", LeadingIcon = Symbol.ContentCut, TrailingText = "Ctrl+X", Action = () => actions++ });
                menu.Add(new Divider());
                menu.Add(paste = new MenuItem { Text = "Paste", IsEnabled = false });
                menu.ItemClicked += i => picked = i;

                return button = new FilledButton { Text = "Edit", Menu = menu };
            });
            AddUntilStep("laid out", () => button.DrawWidth > 0);

            click(() => button);
            AddUntilStep("open", () => menu.IsOpen.Value && cut.IsPresent && cut.DrawHeight == 48);
            AddAssert("below the button, left edges aligned",
                () => cut.ScreenSpaceDrawQuad.TopLeft.Y >= button.ScreenSpaceDrawQuad.BottomLeft.Y - 1
                      && System.Math.Abs(cut.ScreenSpaceDrawQuad.TopLeft.X - button.ScreenSpaceDrawQuad.TopLeft.X) < 1);
            AddAssert("at least 112 wide", () => cut.DrawWidth >= 112);

            click(() => paste);
            AddAssert("disabled item does nothing", () => menu.IsOpen.Value && picked == null);

            click(() => cut);
            AddAssert("picked", () => picked == cut && actions == 1);
            AddUntilStep("closed", () => !menu.IsOpen.Value);

            click(() => button);
            AddUntilStep("open again", () => menu.IsOpen.Value);
            AddStep("click outside", () =>
            {
                InputManager.MoveMouseTo(window.ScreenSpaceDrawQuad.BottomRight - new Vector2(10));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("closed", () => !menu.IsOpen.Value);
        }

        [Test]
        public void DropdownFieldPicksAnOption()
        {
            DropdownField dropdown = null;
            MenuItem japan = null;

            create(() =>
            {
                dropdown = new DropdownField { Label = "Country", Width = 280 };
                dropdown.Add(new MenuItem { Text = "Canada" });
                dropdown.Add(japan = new MenuItem { Text = "Japan" });
                dropdown.Add(new MenuItem { Text = "Korea" });
                return dropdown;
            });
            AddUntilStep("laid out", () => dropdown.DrawWidth == 280);
            AddAssert("nothing selected", () => dropdown.SelectedIndex.Value == -1 && dropdown.SelectedText == string.Empty);

            click(() => dropdown);
            AddUntilStep("menu open, as wide as the field", () => japan.IsPresent && System.Math.Abs(japan.DrawWidth - 280) < 1);

            click(() => japan);
            AddAssert("Japan selected", () => dropdown.SelectedIndex.Value == 1 && dropdown.SelectedText == "Japan");

            AddStep("select from code", () => dropdown.SelectedIndex.Value = 2);
            AddAssert("shows Korea", () => dropdown.SelectedText == "Korea");
        }

        [Test]
        public void AlertDialogAnswers()
        {
            FilledButton owner = null;
            Task<bool?> result = null;

            create(() => owner = new FilledButton { Text = "Owner" });
            AddUntilStep("laid out", () => owner.DrawWidth > 0);

            AlertDialog dialog() => window.PopupLayer.OpenModals.Count == 1 ? window.PopupLayer.OpenModals[0].Dialog as AlertDialog : null;

            void show() => AddStep("show", () => result = AlertDialog.Show(owner, "Delete draft?", "It can't be undone.", "Delete", "Cancel", Symbol.Delete));

            show();
            AddUntilStep("open", () => dialog()?.IsLoaded == true && dialog().Alpha == 1);
            AddAssert("rounded 28, between 280 and 560 wide", () => dialog().CornerRadius == 28 && dialog().DrawWidth >= 280 && dialog().DrawWidth <= 560);
            AddAssert("surface container high", () => close(dialog().Background, ColorScheme.Baseline(false).SurfaceContainerHigh));
            AddAssert("headline and supporting text", () => dialog().Headline.ToString() == "Delete draft?" && dialog().ChildrenOfType<Text>().Any(t => t.Text.ToString() == "It can't be undone."));

            AddStep("Enter", () => InputManager.Key(Key.Enter));
            AddAssert("confirmed", () => result.IsCompleted && result.Result == true);
            AddUntilStep("closed", () => window.PopupLayer.OpenModals.Count == 0);

            show();
            AddUntilStep("open", () => dialog()?.IsLoaded == true);
            AddStep("Escape", () => InputManager.Key(Key.Escape));
            AddAssert("dismissed", () => result.IsCompleted && result.Result == false);
            AddUntilStep("closed", () => window.PopupLayer.OpenModals.Count == 0);

            show();
            AddUntilStep("open", () => dialog()?.IsLoaded == true && dialog().Alpha == 1);
            click(() => dialog().ChildrenOfType<TextButton>().Single(b => b.Text.ToString() == "Cancel"));
            AddAssert("Cancel dismisses", () => result.IsCompleted && result.Result == false);
        }

        [Test]
        public void DialogTakesTheThemeOfItsOwner()
        {
            FilledButton owner = null;

            create(() =>
            {
                var scope = new ThemeScope { IsDark = true };
                scope.Content = owner = new FilledButton { Text = "Owner" };
                return scope;
            });
            AddUntilStep("laid out", () => owner.DrawWidth > 0);
            AddStep("show", () => AlertDialog.Show(owner, "Dark?", "Like the button.", "OK"));
            AddUntilStep("dark surface", () => window.PopupLayer.OpenModals.Count == 1
                                               && window.PopupLayer.OpenModals[0].Dialog is AlertDialog d && d.IsLoaded
                                               && close(d.Background, ColorScheme.Baseline(true).SurfaceContainerHigh));
            AddStep("Enter", () => InputManager.Key(Key.Enter));
        }

        [Test]
        public void SnackbarsQueueAndDismiss()
        {
            SnackbarHost host = null;
            int undone = 0;

            AddStep("create", () =>
            {
                undone = 0;
                host = new SnackbarHost { Content = new Container { RelativeSizeAxes = Axes.Both } };
                Child = window = new Window { Child = host };
            });
            AddUntilStep("loaded", () => host.IsLoaded);

            Snackbar[] shown() => host.ChildrenOfType<Snackbar>().Where(s => s.Alpha > 0).ToArray();

            AddStep("show two", () =>
            {
                host.Show("Photo saved");
                host.Show("Archived", "Undo", () => undone++, duration: SnackbarDuration.Indefinite);
            });
            AddUntilStep("the first shows", () => shown().Length == 1 && shown()[0].Message.ToString() == "Photo saved");
            AddAssert("48 tall, at the bottom", () => shown()[0].DrawHeight == 48
                                                         && System.Math.Abs(shown()[0].ScreenSpaceDrawQuad.BottomLeft.Y - (host.ScreenSpaceDrawQuad.BottomLeft.Y - 16)) < 2);

            AddUntilStep("after 4 seconds the second", () => shown().Length == 1 && shown()[0].Message.ToString() == "Archived");
            click(() => shown()[0].ChildrenOfType<TextButton>().Single());
            AddAssert("action ran", () => undone == 1);
            AddUntilStep("dismissed", () => shown().Length == 0);
        }

        [Test]
        public void PlainTooltipShowsCentredBelow()
        {
            IconButton button = null;

            create(() =>
            {
                button = new IconButton { Icon = Symbol.Share };
                Tooltips.SetPlainTooltip(button, "Share");
                return button;
            });
            AddUntilStep("laid out", () => button.DrawWidth > 0);
            AddAssert("property reads back", () => Tooltips.GetPlainTooltip(button).ToString() == "Share");

            AddStep("hover", () => InputManager.MoveMouseTo(button));
            AddUntilStep("shown", () => window.ChildrenOfType<PlainTooltip>().Any(t => t.IsPresent && t.Alpha > 0.9f));
            AddAssert("centred below", () =>
            {
                var tip = window.ChildrenOfType<PlainTooltip>().First().ChildrenOfType<Text>().First();
                var quad = tip.ScreenSpaceDrawQuad;
                return quad.TopLeft.Y > button.ScreenSpaceDrawQuad.BottomLeft.Y
                       && System.Math.Abs(quad.Centre.X - button.ScreenSpaceDrawQuad.Centre.X) < 2;
            });
        }

        private static bool close(Colour4 a, Colour4 b) => System.Math.Abs(a.R - b.R) < 0.01f && System.Math.Abs(a.G - b.G) < 0.01f && System.Math.Abs(a.B - b.B) < 0.01f;
    }
}
