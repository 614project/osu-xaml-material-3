using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Testing;
using osu.Framework.Utils;
using OsuXaml.Controls;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3.Tests
{
    [HeadlessTest]
    public partial class InputTests : MaterialTestScene
    {
        [Test]
        public void SliderFollowsClicksDragsAndKeys()
        {
            Slider slider = null;
            var bound = new BindableDouble(40);

            AddStep("create", () =>
            {
                slider = new Slider { Width = 400 };
                slider.Value.BindTo(bound);
                Child = slider;
            });
            AddUntilStep("laid out", () => slider.DrawWidth == 400);
            AddAssert("44 tall, for the handle", () => slider.DrawHeight == 44);

            AddStep("click three quarters along", () =>
            {
                InputManager.MoveMouseTo(slider.ToScreenSpace(new Vector2(slider.DrawWidth * 0.75f, 22)));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("about 75", () => Precision.AlmostEquals(bound.Value, 75, 2));

            AddStep("drag to the start", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(slider.ToScreenSpace(new Vector2(-50, 22)));
                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("clamped to 0", () => bound.Value == 0);

            AddStep("Right twice", () =>
            {
                InputManager.Key(Key.Right);
                InputManager.Key(Key.Right);
            });
            AddAssert("focused", () => slider.HasFocus);
            AddAssert("1% steps", () => Precision.AlmostEquals(bound.Value, 2, 0.001));
            AddStep("End", () => InputManager.Key(Key.End));
            AddAssert("maximum", () => bound.Value == 100);

            AddStep("steps of 10", () =>
            {
                slider.StepSize = 10;
                InputManager.MoveMouseTo(slider.ToScreenSpace(new Vector2(slider.DrawWidth * 0.33f, 22)));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("snapped to 30", () => bound.Value == 30);
            AddStep("Left", () => InputManager.Key(Key.Left));
            AddAssert("20", () => bound.Value == 20);

            AddStep("view model sets 90", () => bound.Value = 90);
            AddAssert("slider follows", () => slider.Value.Value == 90);
        }

        [Test]
        public void TextFieldShowsLabelSupportingTextAndCounter()
        {
            TextField field = null;
            var name = new Bindable<string>(string.Empty);

            AddStep("create", () =>
            {
                field = new TextField { Label = "Name", SupportingText = "As on your ID", MaxLength = 20, Width = 300 };
                field.Text.BindTo(name);
                Child = field;
            });
            AddUntilStep("laid out", () => field.DrawWidth == 300);
            AddAssert("56dp field with supporting text below", () => field.DrawHeight > 56 && field.DrawHeight < 90);
            AddAssert("counter shows 0/20", () => hasText(field, "0/20"));
            AddAssert("supporting text shown", () => hasText(field, "As on your ID"));

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(field.ToScreenSpace(new Vector2(150, 28)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("focused", () => field.IsFocused);

            AddStep("view model sets the text", () => name.Value = "Ada");
            AddAssert("counter follows", () => hasText(field, "3/20"));

            AddStep("error", () =>
            {
                field.IsError = true;
                field.ErrorText = "Too short";
            });
            AddAssert("error text replaces supporting text", () => hasText(field, "Too short") && !hasText(field, "As on your ID"));

            AddStep("click elsewhere", () =>
            {
                InputManager.MoveMouseTo(field.ToScreenSpace(new Vector2(150, 200)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("unfocused", () => !field.IsFocused);
        }

        [Test]
        public void OutlinedTextFieldKeepsItsHeight()
        {
            TextField filled = null;
            TextField outlined = null;

            AddStep("create", () =>
            {
                filled = new TextField { Label = "Filled" };
                outlined = new TextField { Label = "Outlined", Variant = TextFieldVariant.Outlined };

                var panel = new StackPanel { Spacing = 8, Width = 300 };
                panel.Add(filled);
                panel.Add(outlined);
                Child = panel;
            });
            AddUntilStep("laid out", () => outlined.DrawWidth > 0);
            AddAssert("both 56 tall without supporting text", () => filled.DrawHeight == 56 && outlined.DrawHeight == 56);
            AddAssert("stretched across the panel", () => filled.DrawWidth == 300 && outlined.DrawWidth == 300);
        }

        [Test]
        public void SearchBarRaisesCommitted()
        {
            SearchBar bar = null;
            string committed = null;

            AddStep("create", () =>
            {
                committed = null;
                bar = new SearchBar { Placeholder = "Search" };
                bar.Committed += q => committed = q;
                Child = bar;
            });
            AddUntilStep("laid out", () => bar.DrawWidth > 0);
            AddAssert("56dp pill", () => bar.DrawHeight == 56);

            AddStep("click and set a query", () =>
            {
                InputManager.MoveMouseTo(bar);
                InputManager.Click(MouseButton.Left);
                bar.Text.Value = "otters";
            });
            AddStep("Enter", () => InputManager.Key(Key.Enter));
            AddAssert("committed", () => committed == "otters");
        }

        private static bool hasText(Drawable root, string text)
        {
            foreach (var t in root.ChildrenOfType<Text>())
            {
                if (t.Text.ToString() == text && t.IsPresent && t.Alpha > 0)
                    return true;
            }

            return false;
        }
    }
}
