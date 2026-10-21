using NUnit.Framework;
using osu.Framework.Testing;
using osu.Framework.Utils;
using OsuXaml.Controls;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3.Tests
{
    [HeadlessTest]
    public partial class ButtonTests : MaterialTestScene
    {
        [Test]
        public void ButtonsTakeMaterialSizes()
        {
            FilledButton filled = null;
            FilledButton withIcon = null;
            IconButton icon = null;
            Fab small = null;
            Fab regular = null;
            Fab large = null;
            ExtendedFab extended = null;

            AddStep("create", () =>
            {
                var panel = new StackPanel { Spacing = 8 };
                panel.Add(filled = new FilledButton { Text = "Save" });
                panel.Add(withIcon = new FilledButton { Text = "Save", Icon = Symbol.Save });
                panel.Add(icon = new IconButton { Icon = Symbol.Edit });
                panel.Add(small = new Fab { FabSize = FabSize.Small, Icon = Symbol.Edit });
                panel.Add(regular = new Fab { Icon = Symbol.Edit });
                panel.Add(large = new Fab { FabSize = FabSize.Large, Icon = Symbol.Edit });
                panel.Add(extended = new ExtendedFab { Icon = Symbol.Edit, Text = "Compose" });
                Child = panel;
            });
            AddUntilStep("laid out", () => filled.DrawWidth > 0 && extended.DrawWidth > 0);

            AddAssert("common buttons are 40 tall and hug their label", () => filled.DrawHeight == 40 && filled.DrawWidth > 60 && filled.DrawWidth < 120);
            // 24dp each side without an icon; 16dp, the 18dp icon and an 8dp gap before the label with one.
            AddAssert("an icon widens the button by 18", () => Precision.AlmostEquals(withIcon.DrawWidth, filled.DrawWidth + 18, 0.5f));
            AddAssert("icon button is 40 square", () => icon.DrawSize == new Vector2(40));
            AddAssert("FABs are 40, 56 and 96", () => small.DrawSize == new Vector2(40) && regular.DrawSize == new Vector2(56) && large.DrawSize == new Vector2(96));
            AddAssert("extended FAB is 56 tall", () => extended.DrawHeight == 56 && extended.DrawWidth > 100);

            AddStep("collapse the extended FAB", () => extended.Expanded = false);
            AddUntilStep("shrinks to a FAB", () => extended.DrawWidth == 56);
        }

        [Test]
        public void ClickRaisesClickAndAction()
        {
            OutlinedButton button = null;
            int clicks = 0;
            int actions = 0;

            AddStep("create", () =>
            {
                clicks = actions = 0;
                button = new OutlinedButton { Text = "Go", Action = () => actions++ };
                button.Click += () => clicks++;
                Child = button;
            });
            AddUntilStep("laid out", () => button.DrawWidth > 0);

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("both raised", () => clicks == 1 && actions == 1);

            AddStep("disable and click", () =>
            {
                button.IsEnabled = false;
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("nothing raised", () => clicks == 1 && actions == 1);

            AddStep("PerformClick while disabled", () => button.PerformClick());
            AddAssert("still nothing", () => clicks == 1);
            AddStep("enable and PerformClick", () =>
            {
                button.IsEnabled = true;
                button.PerformClick();
            });
            AddAssert("clicked", () => clicks == 2 && actions == 2);
        }

        [Test]
        public void ToggleIconButtonFlipsIsSelected()
        {
            IconButton button = null;

            AddStep("create", () => Child = button = new IconButton { Icon = Symbol.Favorite, IsToggle = true });
            AddUntilStep("laid out", () => button.DrawWidth > 0);

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("selected", () => button.IsSelected.Value);
            AddStep("click again", () => InputManager.Click(MouseButton.Left));
            AddAssert("deselected", () => !button.IsSelected.Value);
        }

        [Test]
        public void SegmentedButtonSelectsOneOrSeveral()
        {
            SegmentedButton single = null;
            SegmentedButton multi = null;
            int changes = 0;

            AddStep("create", () =>
            {
                changes = 0;

                single = new SegmentedButton();
                single.Add(new Segment { Text = "Day" });
                single.Add(new Segment { Text = "Week" });
                single.Add(new Segment { Text = "Month" });
                single.SelectionChanged += () => changes++;

                multi = new SegmentedButton { IsMultiSelect = true };
                multi.Add(new Segment { Text = "S" });
                multi.Add(new Segment { Text = "M" });
                multi.Add(new Segment { Text = "L" });

                var panel = new StackPanel { Spacing = 16 };
                panel.Add(single);
                panel.Add(multi);
                Child = panel;
            });
            AddUntilStep("laid out", () => single.DrawWidth > 0 && multi.DrawWidth > 0);

            AddAssert("segments share the width", () => single.Segments[0].DrawWidth == single.Segments[2].DrawWidth);
            AddAssert("first selected", () => single.SelectedIndex.Value == 0 && single.Segments[0].IsSelected.Value);

            AddStep("click Month", () =>
            {
                InputManager.MoveMouseTo(single.Segments[2]);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("Month alone selected", () => single.SelectedIndex.Value == 2 && !single.Segments[0].IsSelected.Value && single.Segments[2].IsSelected.Value);
            AddAssert("change raised", () => changes == 1);

            AddStep("click Month again", () => InputManager.Click(MouseButton.Left));
            AddAssert("stays selected", () => single.SelectedIndex.Value == 2);

            AddStep("click S and L", () =>
            {
                InputManager.MoveMouseTo(multi.Segments[0]);
                InputManager.Click(MouseButton.Left);
                InputManager.MoveMouseTo(multi.Segments[2]);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("both selected", () => multi.Segments[0].IsSelected.Value && !multi.Segments[1].IsSelected.Value && multi.Segments[2].IsSelected.Value);
            AddStep("click L again", () => InputManager.Click(MouseButton.Left));
            AddAssert("L deselected", () => !multi.Segments[2].IsSelected.Value);
        }

        [Test]
        public void ChipsSelectAndRemove()
        {
            FilterChip filter = null;
            InputChip input = null;
            int removed = 0;

            AddStep("create", () =>
            {
                removed = 0;
                filter = new FilterChip { Text = "Vegetarian" };
                input = new InputChip { Text = "Ada Lovelace" };
                input.Removed += () => removed++;

                var panel = new StackPanel { Spacing = 8 };
                panel.Add(filter);
                panel.Add(input);
                Child = panel;
            });
            AddUntilStep("laid out", () => filter.DrawWidth > 0 && input.DrawWidth > 0);
            AddAssert("chips are 32 tall", () => filter.DrawHeight == 32 && input.DrawHeight == 32);

            float unselectedWidth = 0;
            AddStep("click the filter chip", () =>
            {
                unselectedWidth = filter.DrawWidth;
                InputManager.MoveMouseTo(filter);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("selected", () => filter.IsSelected.Value);
            AddUntilStep("makes room for the check", () => filter.DrawWidth > unselectedWidth);

            AddStep("click the input chip's label", () =>
            {
                InputManager.MoveMouseTo(input.ScreenSpaceDrawQuad.TopLeft + new Vector2(30, 16));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("not removed", () => removed == 0);

            AddStep("click its cross", () =>
            {
                InputManager.MoveMouseTo(input.ScreenSpaceDrawQuad.TopRight + new Vector2(-17, 16));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("removed", () => removed == 1);
        }
    }
}
