using System;
using System.Linq;
using System.Runtime.CompilerServices;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What checkboxes, radio buttons and switches share: an indicator with a round state layer, an optional label after it
    /// (clicking the label toggles too), and Space to toggle while focused.
    /// </summary>
    public abstract partial class SelectionControl : MaterialControl
    {
        /// <summary>Holds the indicator, at the start of the control.</summary>
        protected readonly Container Indicator;

        /// <summary>The 40dp round state layer; derived controls place it over their indicator.</summary>
        protected readonly CircularContainer StateCircle;

        protected readonly StateLayer StateLayer;

        private readonly Text label;

        protected SelectionControl(Vector2 indicatorSize)
        {
            InternalChildren = new Drawable[]
            {
                Indicator = new Container
                {
                    Size = indicatorSize,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Child = StateCircle = new CircularContainer
                    {
                        Size = new Vector2(40),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        Child = StateLayer = new StateLayer(),
                    },
                },
                label = new Text(TypeStyle.BodyLarge) { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Alpha = 0 },
            };
        }

        /// <summary>A label after the indicator; clicking it toggles the control too.</summary>
        public LocalisableString Text
        {
            get => label.Text;
            set
            {
                label.Text = value;
                label.Alpha = string.IsNullOrEmpty(value.ToString()) ? 0 : 1;
                InvalidateMeasure();
            }
        }

        /// <summary>Raised after a click (or Space) changed the control.</summary>
        public event Action Click;

        public override bool AcceptsFocus => IsEnabled;

        /// <summary>Moves to the next state, as a click does.</summary>
        public void Toggle()
        {
            if (!IsEnabled)
                return;

            OnToggle();
            Click?.Invoke();
        }

        protected abstract void OnToggle();

        private bool hasLabel => label.Alpha > 0;

        /// <summary>The space between the indicator and the label.</summary>
        protected virtual float LabelGap => 4;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var size = Indicator.Size;

            if (hasLabel)
                size = new Vector2(size.X + LabelGap + label.DrawWidth + 8, Math.Max(size.Y, label.DrawHeight));

            return size;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            label.X = Indicator.Width + LabelGap;
        }

        protected override void UpdateColors(bool animate)
        {
            SetColour(label, ContentColour(ColorRole.OnSurface), animate);
            StateLayer.Enabled = IsEnabled;
            StateLayer.Hovered = IsHovered;
        }

        protected override void OnPressed(Vector2 screenSpacePosition) => StateLayer.PressCentre();

        protected override void OnReleased() => StateLayer.Release();

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsEnabled)
                return false;

            Toggle();
            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (HasFocus && e.Key == Key.Space && !e.Repeat && IsEnabled)
            {
                StateLayer.Flash();
                Toggle();
                return true;
            }

            return base.OnKeyDown(e);
        }
    }

    /// <summary>
    /// Material's checkbox: <c>&lt;m3:Checkbox Text="Remember me" IsChecked="{Bind Remember}" /&gt;</c>. <see cref="IsChecked"/> is a
    /// <c>bool?</c> as in WPF and osu-xaml's CheckBox: null is the indeterminate state (a dash), which clicks reach only with
    /// <see cref="IsThreeState"/>. <see cref="IsError"/> draws it in the error colour.
    /// </summary>
    public partial class Checkbox : SelectionControl
    {
        private const float box_size = 18;

        private readonly Container box;
        private readonly Box fill;
        private readonly Outline outline;
        private readonly Polyline check;
        private readonly Polyline dash;
        private bool isError;

        public Checkbox()
            : base(new Vector2(40))
        {
            Indicator.Add(box = new Container
            {
                Size = new Vector2(box_size),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                CornerRadius = 2,
                Children = new Drawable[]
                {
                    fill = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                    outline = new Outline { Thickness = 2, ShapeRadius = 2 },
                    check = new Polyline
                    {
                        Size = new Vector2(box_size),
                        Points = new PointCollection(new[] { new Vector2(4, 9.2f), new Vector2(7.4f, 12.6f), new Vector2(14, 6) }),
                        Stroke = Colour4.White,
                        StrokeThickness = 2,
                        Alpha = 0,
                    },
                    dash = new Polyline
                    {
                        Size = new Vector2(box_size),
                        Points = new PointCollection(new[] { new Vector2(4, 9), new Vector2(14, 9) }),
                        Stroke = Colour4.White,
                        StrokeThickness = 2,
                        Alpha = 0,
                    },
                },
            });

            IsChecked.BindValueChanged(e =>
            {
                if (IsLoaded)
                    animateMark();

                OnStateChanged();
            });
        }

        /// <summary>Checked, unchecked, or null for indeterminate. Binds two-way with a <c>Bindable&lt;bool?&gt;</c> or <c>Bindable&lt;bool&gt;</c>.</summary>
        public Bindable<bool?> IsChecked { get; } = new Bindable<bool?>(false);

        /// <summary>Whether clicks also reach the indeterminate state (checked, then indeterminate, then unchecked).</summary>
        public bool IsThreeState { get; set; }

        /// <summary>Draws the box in the error colour, for a required choice left unmade.</summary>
        public bool IsError
        {
            get => isError;
            set
            {
                isError = value;
                OnStateChanged();
            }
        }

        protected override void OnToggle()
        {
            if (IsChecked.Disabled)
                return;

            var current = IsChecked.Value;
            IsChecked.Value = current == true ? IsThreeState ? null : false : current == null ? false : true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            applyMark(false);
        }

        private void animateMark() => applyMark(true);

        private void applyMark(bool animate)
        {
            bool on = IsChecked.Value == true;
            bool mixed = IsChecked.Value == null;
            double duration = animate ? Motion.Short4 : 0;

            fill.FadeTo(on || mixed ? 1 : 0, duration, Motion.Standard);
            check.FadeTo(on ? 1 : 0, duration, Motion.Standard);
            dash.FadeTo(mixed ? 1 : 0, duration, Motion.Standard);

            if (on && animate)
                check.ScaleTo(0.4f).ScaleTo(1, Motion.Medium1, Motion.EmphasizedDecelerate);
            if (mixed && animate)
                dash.ScaleTo(new Vector2(0.2f, 1)).ScaleTo(1, Motion.Medium1, Motion.EmphasizedDecelerate);
        }

        protected override void UpdateColors(bool animate)
        {
            base.UpdateColors(animate);

            bool selected = IsChecked.Value != false;
            var accent = isError ? ColorRole.Error : ColorRole.Primary;

            Colour4 container = IsEnabled ? Scheme[accent] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContent);
            Colour4 mark = IsEnabled ? Scheme[isError ? ColorRole.OnError : ColorRole.OnPrimary] : Scheme.Surface;
            Colour4 border = !IsEnabled ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContent)
                : isError ? Scheme.Error
                : IsHovered || IsPressed ? Scheme.OnSurface : Scheme.OnSurfaceVariant;

            SetColour(fill, container, animate);
            SetColour(check, mark, animate);
            SetColour(dash, mark, animate);
            SetColour(outline, selected ? Colour4.Transparent : border, animate);

            StateLayer.Color = isError ? Scheme.Error : selected ? Scheme.Primary : Scheme.OnSurface;
        }
    }

    /// <summary>
    /// Material's radio button: <c>&lt;m3:RadioButton Text="Small" GroupName="Size" /&gt;</c>. Checking one unchecks the others in its
    /// group, which is the buttons with the same <see cref="GroupName"/> in the same window, or without one, the buttons in the
    /// same panel. <see cref="IsChecked"/> binds two-way.
    /// </summary>
    public partial class RadioButton : SelectionControl
    {
        private static readonly ConditionalWeakTable<RadioButton, object> live = new ConditionalWeakTable<RadioButton, object>();

        private readonly Outline ring;
        private readonly Circle dot;

        public RadioButton()
            : base(new Vector2(40))
        {
            Indicator.AddRange(new Drawable[]
            {
                ring = new Outline
                {
                    RelativeSizeAxes = Axes.None,
                    Size = new Vector2(20),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Thickness = 2,
                    ShapeRadius = 10,
                },
                dot = new Circle
                {
                    Size = new Vector2(10),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = Vector2.Zero,
                },
            });

            IsChecked.BindValueChanged(e =>
            {
                if (e.NewValue && IsLoaded)
                    uncheckGroup();

                dot.ScaleTo(e.NewValue ? 1 : 0, IsLoaded ? Motion.Short4 : 0, e.NewValue ? Motion.EmphasizedDecelerate : Motion.EmphasizedAccelerate);
                OnStateChanged();
            });
        }

        public Bindable<bool> IsChecked { get; } = new Bindable<bool>();

        /// <summary>Buttons with the same name form a group anywhere in the window; without one, the buttons of a panel do.</summary>
        public string GroupName { get; set; }

        protected override void OnToggle()
        {
            if (!IsChecked.Disabled)
                IsChecked.Value = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            live.AddOrUpdate(this, null);

            if (IsChecked.Value)
                uncheckGroup();
        }

        private void uncheckGroup()
        {
            object scope = groupScope();

            foreach (var other in live.Select(p => p.Key).ToArray())
            {
                if (other != this && other.IsChecked.Value && !other.IsChecked.Disabled && (other.GroupName ?? string.Empty) == (GroupName ?? string.Empty)
                    && ReferenceEquals(other.groupScope(), scope))
                    other.IsChecked.Value = false;
            }
        }

        private object groupScope()
        {
            if (!string.IsNullOrEmpty(GroupName))
            {
                Drawable root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }

            var parent = Parent;
            return parent is Slot ? parent.Parent : parent;
        }

        protected override void UpdateColors(bool animate)
        {
            base.UpdateColors(animate);

            bool selected = IsChecked.Value;
            Colour4 colour = !IsEnabled ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContent)
                : selected ? Scheme.Primary
                : IsHovered || IsPressed ? Scheme.OnSurface : Scheme.OnSurfaceVariant;

            SetColour(ring, colour, animate);
            SetColour(dot, colour, animate);
            StateLayer.Color = selected ? Scheme.Primary : Scheme.OnSurface;
        }

        protected override void Dispose(bool isDisposing)
        {
            live.Remove(this);
            base.Dispose(isDisposing);
        }
    }

    /// <summary>
    /// Material's switch: <c>&lt;m3:Switch Text="Wi-Fi" IsChecked="{Bind WifiOn}" ShowIcons="True" /&gt;</c>. The handle grows when
    /// selected and while pressed, and can show a check (and a cross when off) with <see cref="ShowIcons"/>.
    /// </summary>
    public partial class Switch : SelectionControl
    {
        private const float track_width = 52;
        private const float track_height = 32;

        // A slight overshoot as the handle lands, as Material's switch does.
        private static readonly CubicBezier handle_easing = new CubicBezier(0.175, 0.885, 0.32, 1.275);

        private readonly Container track;
        private readonly Box trackFill;
        private readonly Outline trackOutline;
        private readonly Container handle;
        private readonly Circle handleFill;
        private readonly Icon onIcon;
        private readonly Icon offIcon;
        private bool showIcons;
        private bool showOffIcon;

        public Switch()
            : base(new Vector2(track_width, track_height))
        {
            Indicator.AddRange(new Drawable[]
            {
                track = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Depth = 1,
                    Children = new Drawable[]
                    {
                        trackFill = new Box { RelativeSizeAxes = Axes.Both },
                        trackOutline = new Outline { Thickness = 2, ShapeRadius = track_height / 2 },
                    },
                },
                handle = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                    Size = new Vector2(40),
                    Children = new Drawable[]
                    {
                        handleFill = new Circle { Anchor = Anchor.Centre, Origin = Anchor.Centre, Size = new Vector2(16) },
                        onIcon = new Icon(Symbol.Check) { Size = 16, ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0 },
                        offIcon = new Icon(Symbol.Close) { Size = 16, ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0 },
                    },
                },
            });

            // The state layer follows the handle.
            Indicator.Remove(StateCircle, false);
            handle.Add(StateCircle);
            handle.ChangeChildDepth(StateCircle, 1);

            IsChecked.BindValueChanged(_ =>
            {
                applyHandle(IsLoaded);
                OnStateChanged();
            });
        }

        public Bindable<bool> IsChecked { get; } = new Bindable<bool>();

        /// <summary>Shows a check in the handle while on (and, with <see cref="ShowOffIcon"/>, a cross while off).</summary>
        public bool ShowIcons
        {
            get => showIcons;
            set
            {
                showIcons = value;
                applyHandle(false);
            }
        }

        /// <summary>Also shows a cross in the handle while off; needs <see cref="ShowIcons"/>.</summary>
        public bool ShowOffIcon
        {
            get => showOffIcon;
            set
            {
                showOffIcon = value;
                applyHandle(false);
            }
        }

        // The track has no state layer around it to space the label, as a checkbox's has.
        protected override float LabelGap => 12;

        protected override void OnToggle()
        {
            if (!IsChecked.Disabled)
                IsChecked.Value = !IsChecked.Value;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            applyHandle(false);
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();

            if (IsLoaded)
                applyHandle(true);
        }

        private void applyHandle(bool animate)
        {
            bool on = IsChecked.Value;
            bool hasIcon = showIcons && (on || showOffIcon);
            float size = IsPressed && IsEnabled ? 28 : on || hasIcon ? 24 : 16;
            float x = on ? track_width - track_height / 2 : track_height / 2;

            double duration = animate ? Motion.Medium1 : 0;

            handle.MoveToX(x, duration, handle_easing);
            handleFill.ResizeTo(size, animate ? Motion.Short4 : 0, Motion.Standard);
            onIcon.FadeTo(showIcons && on ? 1 : 0, animate ? Motion.Short3 : 0);
            offIcon.FadeTo(showIcons && showOffIcon && !on ? 1 : 0, animate ? Motion.Short3 : 0);
        }

        protected override void UpdateColors(bool animate)
        {
            base.UpdateColors(animate);

            bool on = IsChecked.Value;
            bool active = IsHovered || IsPressed;

            Colour4 trackColour, outlineColour, handleColour, iconColour;

            if (!IsEnabled)
            {
                trackColour = on ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer) : Scheme.SurfaceContainerHighest.Opacity(StateOpacity.DisabledContainer);
                outlineColour = on ? Colour4.Transparent : Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer);
                handleColour = on ? Scheme.Surface : Scheme.OnSurface.Opacity(StateOpacity.DisabledContent);
                iconColour = on ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContent) : Scheme.SurfaceContainerHighest.Opacity(StateOpacity.DisabledContent);
            }
            else if (on)
            {
                trackColour = Scheme.Primary;
                outlineColour = Colour4.Transparent;
                handleColour = active ? Scheme.PrimaryContainer : Scheme.OnPrimary;
                iconColour = Scheme.OnPrimaryContainer;
            }
            else
            {
                trackColour = Scheme.SurfaceContainerHighest;
                outlineColour = Scheme.Outline;
                handleColour = active ? Scheme.OnSurfaceVariant : Scheme.Outline;
                iconColour = Scheme.SurfaceContainerHighest;
            }

            SetColour(trackFill, trackColour, animate);
            SetColour(trackOutline, outlineColour, animate);
            SetColour(handleFill, handleColour, animate);
            SetColour(onIcon, iconColour, animate);
            SetColour(offIcon, iconColour, animate);

            StateLayer.Color = on ? Scheme.Primary : Scheme.OnSurface;
        }
    }
}
