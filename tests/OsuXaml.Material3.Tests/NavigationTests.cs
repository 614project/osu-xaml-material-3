using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using OsuXaml.Controls;
using osuTK.Input;

namespace OsuXaml.Material3.Tests
{
    [HeadlessTest]
    public partial class NavigationTests : MaterialTestScene
    {
        [Test]
        public void TabsSwitchContent()
        {
            Tabs tabs = null;
            Text flights = null;
            Text hotels = null;

            AddStep("create", () =>
            {
                tabs = new Tabs { Width = 400 };
                tabs.Add(new Tab { Text = "Flights", Icon = Symbol.Flight, Child = flights = new Text { Text = "Flights content" } });
                tabs.Add(new Tab { Text = "Hotels", Icon = Symbol.Hotel, Child = hotels = new Text { Text = "Hotels content" } });
                Child = tabs;
            });
            AddUntilStep("laid out", () => tabs.DrawWidth == 400);
            AddAssert("first content shown", () => isShown(flights) && !isShown(hotels));

            AddStep("click Hotels", () =>
            {
                InputManager.MoveMouseTo(tabs.Items[1]);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("selected", () => tabs.SelectedIndex.Value == 1);
            AddUntilStep("its content shown", () => isShown(hotels) && !isShown(flights));

            AddStep("view model selects Flights", () => tabs.SelectedIndex.Value = 0);
            AddUntilStep("back", () => isShown(flights));
        }

        [Test]
        public void NavigationBarAndRailSelectItems()
        {
            NavigationBar bar = null;
            NavigationRail rail = null;
            int clicked = -1;

            AddStep("create", () =>
            {
                clicked = -1;
                bar = new NavigationBar { Width = 400 };
                bar.Add(new NavigationBarItem { Icon = Symbol.Home, Text = "Home" });
                bar.Add(new NavigationBarItem { Icon = Symbol.Search, Text = "Search" });
                bar.Add(new NavigationBarItem { Icon = Symbol.Person, Text = "Profile", BadgeText = "3" });
                bar.ItemClicked += i => clicked = i;

                rail = new NavigationRail();
                rail.Add(new NavigationRailItem { Icon = Symbol.Home, Text = "Home" });
                rail.Add(new NavigationRailItem { Icon = Symbol.Settings, Text = "Settings" });

                var panel = new StackPanel { Spacing = 16 };
                panel.Add(bar);
                panel.Add(rail);
                Child = panel;
            });
            AddUntilStep("laid out", () => bar.DrawWidth == 400 && rail.DrawHeight > 0);
            AddAssert("bar is 80 tall, rail 80 wide", () => bar.DrawHeight == 80 && rail.DrawWidth == 80);
            AddAssert("first selected", () => bar.Items[0].IsSelected.Value && !bar.Items[1].IsSelected.Value);
            AddAssert("badge shown", () => bar.Items[2].ChildrenOfType<Badge>().Any(b => b.IsPresent));

            AddStep("click Profile", () =>
            {
                InputManager.MoveMouseTo(bar.Items[2]);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("selected", () => bar.SelectedIndex.Value == 2 && bar.Items[2].IsSelected.Value && !bar.Items[0].IsSelected.Value);
            AddAssert("ItemClicked", () => clicked == 2);

            AddStep("click Settings on the rail", () =>
            {
                InputManager.MoveMouseTo(rail.Items[1]);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("rail selected", () => rail.SelectedIndex.Value == 1);
        }

        [Test]
        public void AppBarsTakeTheirHeights()
        {
            TopAppBar small = null;
            TopAppBar medium = null;
            TopAppBar large = null;
            BottomAppBar bottom = null;
            int navigationClicks = 0;

            AddStep("create", () =>
            {
                navigationClicks = 0;
                small = new TopAppBar { Title = "Small", NavigationIcon = Symbol.ArrowBack };
                small.NavigationClick += () => navigationClicks++;
                medium = new TopAppBar { Title = "Medium", Variant = TopAppBarVariant.Medium };
                large = new TopAppBar { Title = "Large", Variant = TopAppBarVariant.Large };
                bottom = new BottomAppBar();
                bottom.Add(new IconButton { Icon = Symbol.Check });

                var panel = new StackPanel { Width = 400 };
                panel.Add(small);
                panel.Add(medium);
                panel.Add(large);
                panel.Add(bottom);
                Child = panel;
            });
            AddUntilStep("laid out", () => bottom.DrawHeight > 0);
            AddAssert("64, 112, 152 and 80", () => small.DrawHeight == 64 && medium.DrawHeight == 112 && large.DrawHeight == 152 && bottom.DrawHeight == 80);

            AddStep("click the navigation icon", () =>
            {
                InputManager.MoveMouseTo(small.ToScreenSpace(new osuTK.Vector2(28, 32)));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("NavigationClick", () => navigationClicks == 1);
        }

        [Test]
        public void ListItemsTakeTheirHeights()
        {
            ListItem one = null;
            ListItem two = null;
            ListItem three = null;
            int clicks = 0;

            AddStep("create", () =>
            {
                clicks = 0;
                one = new ListItem { Headline = "One line", LeadingIcon = Symbol.Inbox };
                one.Click += () => clicks++;
                two = new ListItem { Headline = "Two lines", SupportingText = "Supporting text" };
                three = new ListItem { Headline = "Three lines", SupportingText = "Supporting text", Overline = "Overline" };

                var panel = new StackPanel { Width = 360 };
                panel.Add(one);
                panel.Add(two);
                panel.Add(three);
                Child = panel;
            });
            AddUntilStep("laid out", () => three.DrawHeight > 0);
            AddAssert("56, 72 and 88", () => one.DrawHeight == 56 && two.DrawHeight == 72 && three.DrawHeight == 88);

            AddStep("click", () =>
            {
                InputManager.MoveMouseTo(one);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("clicked", () => clicks == 1);
        }

        /// <summary>Whether the drawable is in the scene and drawn.</summary>
        private bool isShown(Drawable drawable)
        {
            for (var d = drawable; d != null; d = d.Parent)
            {
                if (!d.IsPresent)
                    return false;

                if (d == this)
                    return true;
            }

            return false;
        }
    }
}
