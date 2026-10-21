using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's menu: a list of choices on a 4dp rounded SurfaceContainer sheet that opens over everything (an osu-xaml
    /// <see cref="Popup"/>). Give a button one and it opens below the button on click:
    /// <code>
    /// &lt;m3:IconButton Icon="MoreVert"&gt;
    ///   &lt;m3:IconButton.Menu&gt;
    ///     &lt;m3:Menu&gt;
    ///       &lt;m3:MenuItem Text="Cut" LeadingIcon="ContentCut" TrailingText="Ctrl+X" /&gt;
    ///       &lt;m3:Divider /&gt;
    ///       &lt;m3:MenuItem Text="Settings" LeadingIcon="Settings" /&gt;
    ///     &lt;/m3:Menu&gt;
    ///   &lt;/m3:IconButton.Menu&gt;
    /// &lt;/m3:IconButton&gt;
    /// </code>
    /// Clicking an item closes the menu; clicking outside or Escape closes it too.
    /// </summary>
    public partial class Menu : Popup
    {
        private readonly ThemedContainer themed;
        private readonly MenuSurface surface;

        [Resolved(canBeNull: true)]
        private MaterialTheme theme { get; set; }

        public Menu()
        {
            Child = themed = new ThemedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = surface = new MenuSurface(),
            };

            VerticalOffset = 2;

            // Registered before the popup's own handler, so the sheet is ready before the popup places it.
            IsOpen.BindValueChanged(e =>
            {
                if (!e.NewValue)
                    return;

                // A menu as wide as its target (a dropdown's) fills the width the popup gives it.
                themed.AutoSizeAxes = MatchTargetWidth ? Axes.Y : Axes.Both;
                surface.RelativeSizeAxes = MatchTargetWidth ? Axes.X : Axes.None;
                if (MatchTargetWidth) surface.Width = 1;
            });
        }

        /// <summary>Adds a <see cref="MenuItem"/>, a <see cref="Divider"/> or any other element; XAML child elements land here.</summary>
        public void Add(Drawable item)
        {
            if (item is MenuItem menuItem)
                menuItem.Menu = this;

            surface.AddItem(item);
        }

        public IEnumerable<Drawable> Items => surface.Items;

        /// <summary>Raised when an item is clicked, before the menu closes.</summary>
        public event Action<MenuItem> ItemClicked;

        /// <summary>The narrowest the menu gets (112 by default); a dropdown sets it to its own width.</summary>
        public float MinWidth
        {
            get => surface.MinWidth;
            set => surface.MinWidth = value;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // The sheet opens in the window's popup layer, outside any theme scope the menu is declared in; it takes that theme along.
            themed.Theme = theme ?? MaterialTheme.Of(this);
        }

        internal void OnItemClicked(MenuItem item)
        {
            ItemClicked?.Invoke(item);
            Close();
        }

        private partial class MenuSurface : MaterialControl
        {
            private readonly ElevationShadow shadow;
            private readonly Container clip;
            private readonly Box background;
            private readonly FillFlowContainer column;

            public MenuSurface()
            {
                InternalChildren = new Drawable[]
                {
                    shadow = new ElevationShadow { ShapeRadius = 4 },
                    clip = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 4,
                        Children = new Drawable[]
                        {
                            background = new Box { RelativeSizeAxes = Axes.Both },
                            column = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding { Vertical = 8 },
                            },
                        },
                    },
                };
            }

            public float MinWidth { get; set; } = 112;

            public IEnumerable<Drawable> Items => column.Children;

            public void AddItem(Drawable item)
            {
                if (item is Divider divider)
                    divider.Margin = new MarginPadding { Vertical = 8 };
                else if (!(item is MenuItem))
                    item.Margin = new MarginPadding { Horizontal = 12 };

                column.Add(item);
                InvalidateMeasure();
            }

            protected override Vector2 MeasureOverride(Vector2 availableSize)
            {
                float width = MinWidth;

                foreach (var item in column.Children)
                {
                    if (item is MenuItem menuItem)
                        width = Math.Max(width, menuItem.DesiredWidth);
                }

                return new Vector2(Math.Min(Math.Max(width, MinWidth), Math.Max(280, MinWidth)), column.DrawHeight);
            }

            protected override bool Interactive => false;

            protected override void UpdateColors(bool animate)
            {
                SetColour(background, Scheme.SurfaceContainer, animate);
                shadow.ShadowColour = Scheme.Shadow;
                shadow.Level = Elevation.Level2;
            }
        }
    }

    /// <summary>
    /// A choice in a <see cref="Menu"/> or <see cref="DropdownField"/>: a 48dp row with a label, optional leading and trailing icons
    /// and trailing text (a keyboard shortcut, say).
    /// </summary>
    public partial class MenuItem : MaterialControl
    {
        private const float height = 48;
        private const float padding = 12;
        private const float gap = 12;

        private readonly StateLayer stateLayer;
        private readonly Box selectedFill;
        private readonly Icon leading;
        private readonly Text label;
        private readonly Text trailingText;
        private readonly Icon trailing;
        private bool isSelected;

        public MenuItem()
        {
            RelativeSizeAxes = Axes.X;
            Height = height;

            InternalChildren = new Drawable[]
            {
                selectedFill = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                stateLayer = new StateLayer(),
                leading = new Icon { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, X = padding, Alpha = 0 },
                label = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                trailingText = new Text(TypeStyle.LabelLarge) { ColorRole = null, Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, X = -padding },
                trailing = new Icon { ColorRole = null, Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, X = -padding, Alpha = 0 },
            };
        }

        internal Menu Menu { get; set; }

        public LocalisableString Text
        {
            get => label.Text;
            set => label.Text = value;
        }

        public Symbol LeadingIcon
        {
            get => leading.Symbol;
            set
            {
                leading.Symbol = value;
                leading.Alpha = value == Symbol.None ? 0 : 1;
            }
        }

        public Symbol TrailingIcon
        {
            get => trailing.Symbol;
            set
            {
                trailing.Symbol = value;
                trailing.Alpha = value == Symbol.None ? 0 : 1;
            }
        }

        /// <summary>Text at the end of the row, such as a keyboard shortcut.</summary>
        public LocalisableString TrailingText
        {
            get => trailingText.Text;
            set => trailingText.Text = value;
        }

        /// <summary>Any value the item stands for, for the app's own use.</summary>
        public object Value { get; set; }

        /// <summary>Marks the current choice (a dropdown's selected option) with the secondary container colour.</summary>
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                OnStateChanged();
            }
        }

        /// <summary>Raised when the item is clicked.</summary>
        public event Action Click;

        /// <summary>The same as <see cref="Click"/>, for bindings.</summary>
        public Action Action { get; set; }

        internal float DesiredWidth
        {
            get
            {
                float width = padding + (leading.Alpha > 0 ? 24 + gap : 0) + label.DrawWidth + padding;

                if (!string.IsNullOrEmpty(trailingText.Text.ToString()))
                    width += 24 + trailingText.DrawWidth;
                if (trailing.Alpha > 0)
                    width += gap + 24;

                return width;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(DesiredWidth, height);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            label.X = padding + (leading.Alpha > 0 ? 24 + gap : 0);
            trailingText.X = -(padding + (trailing.Alpha > 0 ? 24 + gap : 0));
        }

        protected override void OnPressed(Vector2 screenSpacePosition) => stateLayer.Press(screenSpacePosition);

        protected override void OnReleased() => stateLayer.Release();

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsEnabled)
                return false;

            Click?.Invoke();
            Action?.Invoke();
            Menu?.OnItemClicked(this);
            return true;
        }

        protected override void UpdateColors(bool animate)
        {
            selectedFill.Alpha = isSelected ? 1 : 0;
            SetColour(selectedFill, Scheme.SecondaryContainer, animate);

            SetColour(label, ContentColour(isSelected ? ColorRole.OnSecondaryContainer : ColorRole.OnSurface), animate);
            SetColour(leading, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(trailing, ContentColour(ColorRole.OnSurfaceVariant), animate);
            SetColour(trailingText, ContentColour(ColorRole.OnSurfaceVariant), animate);

            stateLayer.Color = Scheme.OnSurface;
            stateLayer.Hovered = IsHovered;
            stateLayer.Enabled = IsEnabled;
        }
    }

    /// <summary>
    /// Material's exposed dropdown menu: a read-only text field that opens a menu of options below itself.
    /// <code>
    /// &lt;m3:DropdownField Label="Size" SelectedIndex="{Bind Size}"&gt;
    ///   &lt;m3:MenuItem Text="Small" /&gt;&lt;m3:MenuItem Text="Medium" /&gt;&lt;m3:MenuItem Text="Large" /&gt;
    /// &lt;/m3:DropdownField&gt;
    /// </code>
    /// The field shows the selected option's text.
    /// </summary>
    public partial class DropdownField : MaterialControl
    {
        private readonly TextField textField;
        private readonly Menu menu;
        private readonly List<MenuItem> options = new List<MenuItem>();

        public DropdownField()
        {
            InternalChildren = new Drawable[]
            {
                textField = new TextField { RelativeSizeAxes = Axes.X, TrailingIcon = Symbol.ArrowDropDown, IsReadOnly = true },
                new ClickCatcher(this) { RelativeSizeAxes = Axes.X, Height = 56 },
                menu = new Menu { MatchTargetWidth = true },
            };

            menu.PlacementTarget = textField;
            menu.Opened += () => onMenuToggled(true);
            menu.Closed += () => onMenuToggled(false);
            menu.ItemClicked += item => SelectedIndex.Value = options.IndexOf(item);

            SelectedIndex.BindValueChanged(_ => applySelection(), true);
        }

        public TextFieldVariant Variant
        {
            get => textField.Variant;
            set => textField.Variant = value;
        }

        public LocalisableString Label
        {
            get => textField.Label;
            set => textField.Label = value;
        }

        public LocalisableString SupportingText
        {
            get => textField.SupportingText;
            set => textField.SupportingText = value;
        }

        public Symbol LeadingIcon
        {
            get => textField.LeadingIcon;
            set => textField.LeadingIcon = value;
        }

        /// <summary>The selected option, or -1 for none. Binds two-way.</summary>
        public Bindable<int> SelectedIndex { get; } = new Bindable<int>(-1);

        /// <summary>The selected option's text.</summary>
        public string SelectedText => textField.Text.Value;

        public void Add(MenuItem option)
        {
            options.Add(option);
            menu.Add(option);
            applySelection();
        }

        private void applySelection()
        {
            for (int i = 0; i < options.Count; i++)
                options[i].IsSelected = i == SelectedIndex.Value;

            int index = SelectedIndex.Value;
            textField.Text.Value = index >= 0 && index < options.Count ? options[index].Text.ToString() : string.Empty;
        }

        private void onMenuToggled(bool open)
        {
            textField.ShowsFocus = open;
            textField.TrailingIcon = open ? Symbol.ArrowDropUp : Symbol.ArrowDropDown;
        }

        private void toggle()
        {
            if (IsEnabled)
                menu.Toggle();
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            textField.IsEnabled = IsEnabled;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(210, textField.DrawHeight);

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
        }

        /// <summary>Takes clicks on the textField (so it never gets a caret) while letting hover through.</summary>
        private partial class ClickCatcher : Drawable
        {
            private readonly DropdownField owner;

            public ClickCatcher(DropdownField owner)
            {
                this.owner = owner;
            }

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e)
            {
                owner.toggle();
                return true;
            }
        }
    }
}
