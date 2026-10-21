using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using OsuXaml.Controls;
using osuTK;
using osuTK.Input;

namespace OsuXaml.Material3
{
    /// <summary>
    /// What the components share: the theme's colour scheme (recolouring them when it changes), hover and press tracking, an
    /// enabled state, and sizing as osu-xaml layout elements, so they sit in Grid, StackPanel and friends like the built-in controls.
    /// Outside a panel a component sizes itself to its content on the axes it has no size for.
    /// </summary>
    public abstract partial class MaterialControl : CompositeDrawable, ILayoutElement
    {
        [Resolved(canBeNull: true)]
        private MaterialTheme cachedTheme { get; set; }

        private IBindable<ColorScheme> scheme = MaterialTheme.Default.Scheme.GetBoundCopy();

        // A child's size change can change the size this control asks for.
        private readonly LayoutValue childSize = new LayoutValue(Invalidation.DrawSize | Invalidation.Presence, InvalidationSource.Child);

        private bool isEnabled = true;
        private bool sizeToContentX;
        private bool sizeToContentY;

        protected MaterialControl()
        {
            AddLayout(childSize);
        }

        /// <summary>The scheme in use.</summary>
        protected ColorScheme Scheme => scheme.Value;

        /// <summary>Whether the pointer is pressed on the control.</summary>
        protected bool IsPressed { get; private set; }

        /// <summary>A disabled control ignores input and draws in Material's disabled colours.</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled == value)
                    return;

                isEnabled = value;

                if (!value)
                    IsPressed = false;

                OnStateChanged();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme.UnbindAll();
            scheme = (cachedTheme ?? MaterialTheme.Default).Scheme.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            sizeToContentX = !(Parent is Slot) && !RelativeSizeAxes.HasFlag(Axes.X) && !AutoSizeAxes.HasFlag(Axes.X) && Width == 0;
            sizeToContentY = !(Parent is Slot) && !RelativeSizeAxes.HasFlag(Axes.Y) && !AutoSizeAxes.HasFlag(Axes.Y) && Height == 0;

            scheme.BindValueChanged(_ => UpdateColors(true));
            UpdateColors(false);
        }

        /// <summary>Applies the scheme's colours for the current state, gliding there when <paramref name="animate"/> is set.</summary>
        protected abstract void UpdateColors(bool animate);

        /// <summary>Called when hover, press or the enabled state change. Recolours by default.</summary>
        protected virtual void OnStateChanged()
        {
            if (IsLoaded)
                UpdateColors(true);
        }

        /// <summary>Sets a drawable's colour, cross-fading to it when animating.</summary>
        protected static void SetColour(Drawable drawable, Colour4 colour, bool animate)
        {
            if (animate)
                drawable.FadeColour(colour, Motion.Short4, Motion.Standard);
            else
            {
                drawable.ClearTransforms(false, nameof(Colour));
                drawable.Colour = colour;
            }
        }

        /// <summary>A role's colour, or the disabled content colour (OnSurface at 38%) while disabled.</summary>
        protected Colour4 ContentColour(ColorRole role) => isEnabled ? Scheme[role] : Scheme.OnSurface.Opacity(StateOpacity.DisabledContent);

        #region Input

        /// <summary>Whether the control reacts to the pointer; a non-interactive one lets hover through to what's behind it.</summary>
        protected virtual bool Interactive => true;

        protected override bool OnHover(HoverEvent e)
        {
            OnStateChanged();
            return Interactive;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            OnStateChanged();
            base.OnHoverLost(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left || !isEnabled || !Interactive)
                return base.OnMouseDown(e);

            IsPressed = true;
            OnPressed(e.ScreenSpaceMousePosition);
            OnStateChanged();
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (e.Button == MouseButton.Left && IsPressed)
            {
                IsPressed = false;
                OnReleased();
                OnStateChanged();
            }

            base.OnMouseUp(e);
        }

        /// <summary>The pointer went down on the control.</summary>
        protected virtual void OnPressed(Vector2 screenSpacePosition)
        {
        }

        /// <summary>The pointer was released.</summary>
        protected virtual void OnReleased()
        {
        }

        #endregion

        #region Layout

        /// <summary>True: a control reports changes to what it measures by calling <see cref="InvalidateMeasure"/>, or through its children's sizes.</summary>
        public virtual bool ReportsMeasureChanges => true;

        public Vector2 Measure(Vector2 availableSize)
        {
            try
            {
                return MeasureOverride(availableSize);
            }
            finally
            {
                childSize.Validate();
            }
        }

        /// <summary>The size the control would like within <paramref name="availableSize"/> (which may be infinite).</summary>
        protected abstract Vector2 MeasureOverride(Vector2 availableSize);

        /// <summary>Tells the panels around the control that it may want another size.</summary>
        protected void InvalidateMeasure() => Layout.InvalidateMeasure(this);

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool result = base.OnInvalidate(invalidation, source);

            if (source == InvalidationSource.Child && (invalidation & (Invalidation.DrawSize | Invalidation.Presence)) != 0)
                Layout.InvalidateMeasure(this);

            return result;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (sizeToContentX || sizeToContentY)
            {
                var desired = Measure(new Vector2(sizeToContentX ? float.PositiveInfinity : DrawWidth, sizeToContentY ? float.PositiveInfinity : DrawHeight));
                if (sizeToContentX) Width = desired.X;
                if (sizeToContentY) Height = desired.Y;
            }
        }

        #endregion

        /// <summary>Clamps a level to Material's 0 to 5.</summary>
        protected static int ClampLevel(int level) => Math.Clamp(level, 0, 5);
    }
}
