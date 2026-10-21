using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What Material's buttons share: a shaped container that can cast an elevation shadow and have an outline, a state layer with
    /// ripples over it, content above that, and <see cref="Click"/>. Derived buttons choose the colours, sizes and shadows.
    /// </summary>
    public abstract partial class ButtonBase : MaterialControl
    {
        protected readonly ElevationShadow Shadow;
        protected readonly Container Chrome;
        protected readonly Box ContainerBox;
        protected readonly StateLayer StateLayer;
        protected readonly Outline OutlineShape;

        /// <summary>Holds the button's icon and label, above the state layer and clipped to the shape.</summary>
        protected readonly Container ContentLayer;

        private ShapeCorner shape = ShapeCorner.Full;
        private bool pressedForClick;

        /// <summary>Raised when the button is clicked.</summary>
        public event Action Click;

        /// <summary>The same as <see cref="Click"/>, as a property for bindings: <c>Action="{Bind Save}"</c>.</summary>
        public Action Action { get; set; }

        protected ButtonBase()
        {
            InternalChildren = new Drawable[]
            {
                Shadow = new ElevationShadow(),
                Chrome = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        ContainerBox = new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Transparent },
                        StateLayer = new StateLayer(),
                        OutlineShape = new Outline { Alpha = 0 },
                        ContentLayer = new Container { RelativeSizeAxes = Axes.Both },
                    },
                },
            };

            // Buttons hug their content in panels, as Material lays them out; HorizontalAlignment="Stretch" widens one.
            Controls.Layout.SetHorizontalAlignment(this, Controls.HorizontalAlignment.Left);
        }

        /// <summary>The corner style; Full (a pill or circle) for most buttons.</summary>
        public ShapeCorner Shape
        {
            get => shape;
            set => shape = value;
        }

        /// <summary>Clicks the button as the pointer would, ripple included.</summary>
        public void PerformClick()
        {
            if (IsEnabled)
                TriggerClick();
        }

        protected override void OnPressed(Vector2 screenSpacePosition)
        {
            pressedForClick = true;
            StateLayer.Press(screenSpacePosition);
        }

        protected override void OnReleased() => StateLayer.Release();

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsEnabled)
                return false;

            // Clicks that didn't come from the pointer (TriggerClick, Enter in a dialog) still ripple.
            if (!pressedForClick)
                StateLayer.Flash();

            pressedForClick = false;
            OnClicked();
            Click?.Invoke();
            Action?.Invoke();

            // As with osu-xaml's buttons, an IsCancel button closes the dialog it is in.
            if (Controls.DialogService.GetIsCancel(this))
                findDialog()?.Close();

            if (Menu != null)
                Menu.Toggle();

            return true;
        }

        private Controls.Dialog findDialog()
        {
            for (var p = Parent; p != null; p = p.Parent)
            {
                if (p is Controls.Dialog dialog)
                    return dialog;
            }

            return null;
        }

        private Menu menu;

        /// <summary>A menu the button opens below itself when clicked: <c>&lt;m3:FilledButton.Menu&gt;&lt;m3:Menu&gt;...</c>.</summary>
        public Menu Menu
        {
            get => menu;
            set
            {
                if (menu != null)
                    RemoveInternal(menu, true);

                menu = value;

                if (menu != null)
                {
                    menu.PlacementTarget ??= this;
                    AddInternal(menu);
                }
            }
        }

        /// <summary>Called on a click before <see cref="Click"/> is raised (toggles flip here).</summary>
        protected virtual void OnClicked()
        {
        }

        protected override void OnStateChanged()
        {
            StateLayer.Enabled = IsEnabled;
            StateLayer.Hovered = IsHovered;
            base.OnStateChanged();
        }

        protected override void Update()
        {
            base.Update();

            float radius = Material3.Shape.Radius(shape, DrawSize);
            Chrome.CornerRadius = radius;
            Shadow.ShapeRadius = radius;
            OutlineShape.ShapeRadius = radius;
        }

        /// <summary>Paints the container, outline, state layer and shadow for the current state.</summary>
        protected void ApplyChrome(Colour4 container, Colour4 content, Colour4? outline, int level, bool animate)
        {
            SetColour(ContainerBox, container, animate);
            StateLayer.Color = content;

            OutlineShape.Alpha = outline != null ? 1 : 0;
            if (outline is Colour4 o)
                SetColour(OutlineShape, o, animate);

            Shadow.ShadowColour = Scheme[ColorRole.Shadow];
            Shadow.SetLevel(IsEnabled ? level : 0, animate);
        }
    }
}
