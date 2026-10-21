using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3
{
    /// <summary>
    /// A themed area holding one piece of content: a container colour role, a corner shape, an elevation shadow and an optional
    /// outline. <c>&lt;m3:Surface Role="SurfaceContainer" Shape="Large" Elevation="1"&gt;...&lt;/m3:Surface&gt;</c>.
    /// Text content takes the role's content colour (OnSurface on surfaces, OnPrimaryContainer on PrimaryContainer, and so on).
    /// </summary>
    public partial class Surface : ContentControl
    {
        protected readonly ElevationShadow Shadow;
        protected readonly Outline OutlineShape;

        private ColorRole? role = ColorRole.Surface;
        private ColorRole? outlineRole;
        private ShapeCorner shape;
        private int elevation;

        [Resolved(canBeNull: true)]
        private MaterialTheme theme { get; set; }

        private IBindable<ColorScheme> scheme;

        public Surface()
        {
            AddInternal(Shadow = new ElevationShadow { Depth = 1 });
            Chrome.Add(OutlineShape = new Outline { Alpha = 0, Depth = -1 });
        }

        /// <summary>The container colour; Surface by default, null for none.</summary>
        public ColorRole? Role
        {
            get => role;
            set
            {
                role = value;
                ApplyColours(false);
            }
        }

        /// <summary>An outline in this role (Outline or OutlineVariant), or null (the default) for none.</summary>
        public ColorRole? OutlineRole
        {
            get => outlineRole;
            set
            {
                outlineRole = value;
                ApplyColours(false);
            }
        }

        /// <summary>The corner style; None by default.</summary>
        public ShapeCorner Shape
        {
            get => shape;
            set => shape = value;
        }

        /// <summary>The elevation level, 0 to 5, which casts Material's shadows.</summary>
        public int Elevation
        {
            get => elevation;
            set
            {
                elevation = Math.Clamp(value, 0, 5);
                ApplyColours(false);
            }
        }

        /// <summary>The scheme in use, once loaded.</summary>
        protected ColorScheme Scheme => scheme?.Value ?? MaterialTheme.Default.Scheme.Value;

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme = (theme ?? MaterialTheme.Default).Scheme.GetBoundCopy();
            scheme.BindValueChanged(_ => ApplyColours(IsLoaded), true);
        }

        protected override Drawable CreateTextContent(LocalisableString text) => new Text(TypeStyle.BodyMedium) { Text = text, ColorRole = null, Colour = Foreground };

        /// <summary>The container role, outline role and elevation for the current state.</summary>
        protected virtual (ColorRole? Container, ColorRole? Outline, int Level) CurrentStyle => (role, outlineRole, elevation);

        /// <summary>Applies the scheme's colours and the shadow for the current state.</summary>
        protected virtual void ApplyColours(bool animate)
        {
            if (scheme == null)
                return;

            var style = CurrentStyle;
            var current = Scheme;

            if (style.Container is ColorRole c)
            {
                BackgroundBox.Alpha = 1;
                setColour(BackgroundBox, current[c], animate);
                Foreground = current[ColorScheme.ContentRoleFor(c)];
            }
            else
            {
                BackgroundBox.Alpha = 0;
                Foreground = current.OnSurface;
            }

            OutlineShape.Alpha = style.Outline != null ? 1 : 0;
            if (style.Outline is ColorRole o)
                setColour(OutlineShape, current[o], animate);

            Shadow.ShadowColour = current.Shadow;
            Shadow.SetLevel(style.Level, animate);
        }

        private static void setColour(Drawable drawable, Colour4 colour, bool animate)
        {
            drawable.ClearTransforms(false, nameof(Colour));

            if (animate)
                drawable.FadeColour(colour, Motion.Medium2, Motion.Standard);
            else
                drawable.Colour = colour;
        }

        protected override void Update()
        {
            base.Update();

            float radius = Material3.Shape.Radius(shape, DrawSize);
            CornerRadius = radius;
            Shadow.ShapeRadius = radius;
            OutlineShape.ShapeRadius = radius;
        }
    }

    /// <summary>The three styles of <see cref="Card"/>.</summary>
    public enum CardVariant
    {
        /// <summary>SurfaceContainerLow with a level 1 shadow.</summary>
        Elevated,

        /// <summary>SurfaceContainerHighest, flat.</summary>
        Filled,

        /// <summary>Surface with an OutlineVariant outline.</summary>
        Outlined,
    }

    /// <summary>
    /// Material's card: content about one subject in a 12dp rounded container. <c>&lt;m3:Card Variant="Outlined"&gt;...&lt;/m3:Card&gt;</c>.
    /// A card with <see cref="IsClickable"/> (or a <see cref="Click"/> handler) shows hover and press feedback and raises
    /// <see cref="Click"/>; hovering an elevated or filled card lifts it a level. 16dp padding by default.
    /// </summary>
    public partial class Card : Surface
    {
        private readonly StateLayer stateLayer;
        private CardVariant variant;
        private bool isClickable;
        private bool isEnabled = true;
        private bool hovered;
        private bool pressed;

        public event Action Click;

        public Card()
        {
            Shape = ShapeCorner.Medium;
            Padding = new MarginPadding(16);

            // The state layer sits between the container and the content.
            Chrome.ChangeChildDepth(BackgroundBox, 2);
            Chrome.Add(stateLayer = new StateLayer { Depth = 1 });
        }

        public CardVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                ApplyColours(false);
            }
        }

        /// <summary>Whether the card reacts to the pointer and raises <see cref="Click"/>. Adding a Click handler sets it.</summary>
        public bool IsClickable
        {
            get => isClickable || Click != null || Action != null;
            set => isClickable = value;
        }

        /// <summary>The same as <see cref="Click"/>, for bindings.</summary>
        public Action Action { get; set; }

        /// <summary>A disabled card takes no clicks and fades its content to 38%.</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                stateLayer.Enabled = value;
                Chrome.FadeTo(value ? 1 : StateOpacity.DisabledContent, Motion.Short4, Motion.Standard);
                ApplyColours(IsLoaded);
            }
        }

        protected override (ColorRole? Container, ColorRole? Outline, int Level) CurrentStyle
        {
            get
            {
                int lift = IsClickable && isEnabled && hovered && !pressed ? 1 : 0;

                return variant switch
                {
                    CardVariant.Filled => (ColorRole.SurfaceContainerHighest, null, 0 + lift),
                    CardVariant.Outlined => (ColorRole.Surface, ColorRole.OutlineVariant, 0 + lift),
                    _ => (ColorRole.SurfaceContainerLow, null, (isEnabled ? 1 : 0) + lift),
                };
            }
        }

        protected override void ApplyColours(bool animate)
        {
            base.ApplyColours(animate);
            stateLayer.Color = Scheme.OnSurface;
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (!IsClickable)
                return false;

            hovered = true;
            stateLayer.Hovered = true;
            ApplyColours(true);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hovered = false;
            stateLayer.Hovered = false;
            ApplyColours(true);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (!IsClickable || !isEnabled || e.Button != MouseButton.Left)
                return false;

            pressed = true;
            stateLayer.Press(e.ScreenSpaceMousePosition);
            ApplyColours(true);
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (!pressed)
                return;

            pressed = false;
            stateLayer.Release();
            ApplyColours(true);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsClickable || !isEnabled)
                return false;

            Click?.Invoke();
            Action?.Invoke();
            return true;
        }
    }

    /// <summary>
    /// A thin line that groups content: <c>&lt;m3:Divider /&gt;</c> across a column, <c>Orientation="Vertical"</c> down a row.
    /// 1dp in OutlineVariant; <see cref="InsetStart"/> and <see cref="InsetEnd"/> leave space at the ends (16dp for list insets).
    /// </summary>
    public partial class Divider : osu.Framework.Graphics.Containers.CompositeDrawable, ILayoutElement
    {
        private readonly osu.Framework.Graphics.Shapes.Box line;
        private Orientation orientation = Orientation.Horizontal;
        private float thickness = 1;
        private float insetStart;
        private float insetEnd;

        [Resolved(canBeNull: true)]
        private MaterialTheme theme { get; set; }

        private IBindable<ColorScheme> scheme;

        public Divider()
        {
            InternalChild = line = new osu.Framework.Graphics.Shapes.Box { RelativeSizeAxes = Axes.Both };
            Colour = MaterialTheme.Default.Scheme.Value.OutlineVariant;
            apply();
        }

        public Orientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                apply();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = value;
                apply();
            }
        }

        public float InsetStart
        {
            get => insetStart;
            set
            {
                insetStart = value;
                apply();
            }
        }

        public float InsetEnd
        {
            get => insetEnd;
            set
            {
                insetEnd = value;
                apply();
            }
        }

        private void apply()
        {
            bool horizontal = orientation == Orientation.Horizontal;
            Padding = horizontal ? new MarginPadding { Left = insetStart, Right = insetEnd } : new MarginPadding { Top = insetStart, Bottom = insetEnd };

            if (IsLoaded)
                applyOwnSize();

            Layout.InvalidateMeasure(this);
        }

        // In a panel the slot sizes the divider from Measure; elsewhere it spans its parent.
        private void applyOwnSize()
        {
            if (Parent is Slot)
                return;

            bool horizontal = orientation == Orientation.Horizontal;
            RelativeSizeAxes = horizontal ? Axes.X : Axes.Y;
            Size = horizontal ? new Vector2(1, thickness) : new Vector2(thickness, 1);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            applyOwnSize();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme = (theme ?? MaterialTheme.Default).Scheme.GetBoundCopy();
            scheme.BindValueChanged(e => this.FadeColour(e.NewValue.OutlineVariant, IsLoaded ? Motion.Medium2 : 0), true);
        }

        public bool ReportsMeasureChanges => true;

        public Vector2 Measure(Vector2 availableSize)
            => orientation == Orientation.Horizontal ? new Vector2(insetStart + insetEnd, thickness) : new Vector2(thickness, insetStart + insetEnd);
    }
}
