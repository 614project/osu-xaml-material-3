using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Testing;
using OsuXaml.Controls;
using osuTK.Input;

namespace OsuXaml.Material3.Tests
{
    [HeadlessTest]
    public partial class SelectionTests : MaterialTestScene
    {
        [Test]
        public void CheckboxTogglesByClickAndSpace()
        {
            Checkbox box = null;
            var bound = new Bindable<bool?>(false);
            int clicks = 0;

            AddStep("create", () =>
            {
                clicks = 0;
                box = new Checkbox { Text = "Remember me" };
                box.IsChecked.BindTo(bound);
                box.Click += () => clicks++;
                Child = box;
            });
            AddUntilStep("laid out", () => box.DrawWidth > 0);
            AddAssert("label beside the box", () => box.DrawWidth > 100 && box.DrawHeight == 40);

            AddStep("click the label", () =>
            {
                InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.TopRight + new osuTK.Vector2(-30, 20));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("checked", () => bound.Value == true && clicks == 1);

            AddStep("Space", () => InputManager.Key(Key.Space));
            AddAssert("unchecked", () => bound.Value == false && clicks == 2);

            AddStep("view model sets null", () => bound.Value = null);
            AddAssert("indeterminate", () => box.IsChecked.Value == null);
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("indeterminate clicks to unchecked", () => bound.Value == false);

            AddStep("disable and click", () =>
            {
                box.IsEnabled = false;
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("unchanged", () => bound.Value == false);
        }

        [Test]
        public void ThreeStateCheckboxCycles()
        {
            Checkbox box = null;

            AddStep("create", () => Child = box = new Checkbox { IsThreeState = true });
            AddUntilStep("laid out", () => box.DrawWidth > 0);
            AddAssert("just the 40dp target", () => box.DrawWidth == 40);

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(box);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("checked", () => box.IsChecked.Value == true);
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("indeterminate", () => box.IsChecked.Value == null);
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("unchecked", () => box.IsChecked.Value == false);
        }

        [Test]
        public void RadioButtonsInAGroupExcludeEachOther()
        {
            RadioButton small = null;
            RadioButton medium = null;
            RadioButton other = null;

            AddStep("create", () =>
            {
                small = new RadioButton { Text = "Small", GroupName = "size" };
                medium = new RadioButton { Text = "Medium", GroupName = "size" };
                other = new RadioButton { Text = "Other group", GroupName = "colour" };
                medium.IsChecked.Value = true;
                other.IsChecked.Value = true;

                var panel = new StackPanel();
                panel.Add(small);
                panel.Add(medium);
                panel.Add(other);
                Child = panel;
            });
            AddUntilStep("laid out", () => other.DrawWidth > 0);

            AddStep("click Small", () =>
            {
                InputManager.MoveMouseTo(small);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("Small replaces Medium", () => small.IsChecked.Value && !medium.IsChecked.Value);
            AddAssert("other group untouched", () => other.IsChecked.Value);

            AddStep("click Small again", () => InputManager.Click(MouseButton.Left));
            AddAssert("stays checked", () => small.IsChecked.Value);
        }

        [Test]
        public void SwitchToggles()
        {
            Switch toggle = null;
            var bound = new BindableBool();

            AddStep("create", () =>
            {
                toggle = new Switch { Text = "Wi-Fi", ShowIcons = true };
                toggle.IsChecked.BindTo(bound);
                Child = toggle;
            });
            AddUntilStep("laid out", () => toggle.DrawWidth > 0);
            AddAssert("52 by 32 track", () => toggle.DrawHeight >= 32 && toggle.DrawWidth > 52 + 12);

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(toggle);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("on", () => bound.Value);
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("off", () => !bound.Value);
        }
    }
}
