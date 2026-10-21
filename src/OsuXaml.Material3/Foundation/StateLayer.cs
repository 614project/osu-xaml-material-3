using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's interaction feedback, drawn over a component's container and under its content: a layer in the content colour
    /// at the hover (8%) or focus (10%) opacity, and a ripple (10%) that spreads from where it was pressed and fades on release.
    /// The component tells it the state; it must sit inside the component's masked, shaped container.
    /// </summary>
    public partial class StateLayer : CompositeDrawable
    {
        private readonly Box layer;
        private readonly Container ripples;
        private Circle activeRipple;
        private double rippleStart;

        private bool hovered;
        private bool focused;
        private bool dragged;
        private bool enabled = true;

        public StateLayer()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                layer = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                ripples = new Container { RelativeSizeAxes = Axes.Both },
            };
        }

        /// <summary>The colour of the layer and ripples: the content colour of the component.</summary>
        public Colour4 Color
        {
            get => layer.Colour;
            set
            {
                layer.Colour = value;
                ripples.Colour = value;
            }
        }

        public bool Hovered
        {
            get => hovered;
            set
            {
                hovered = value;
                update();
            }
        }

        public bool Focused
        {
            get => focused;
            set
            {
                focused = value;
                update();
            }
        }

        public bool Dragged
        {
            get => dragged;
            set
            {
                dragged = value;
                update();
            }
        }

        /// <summary>A disabled component shows no feedback.</summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;

                if (!value)
                    Release();

                update();
            }
        }

        /// <summary>Starts a ripple from a point in screen space; <see cref="Release"/> fades it.</summary>
        public void Press(Vector2 screenSpacePosition) => press(ToLocalSpace(screenSpacePosition));

        /// <summary>Starts a ripple from the centre, as keyboard activation does.</summary>
        public void PressCentre() => press(DrawSize / 2);

        private void press(Vector2 origin)
        {
            if (!enabled)
                return;

            Release();

            // The ripple grows to cover the farthest corner, drifting towards the centre as it does.
            float radius = new[] { Vector2.Zero, new Vector2(DrawWidth, 0), new Vector2(0, DrawHeight), DrawSize }
                           .Select(c => Vector2.Distance(c, DrawSize / 2)).Max() + 2;

            ripples.Add(activeRipple = new Circle
            {
                Origin = Anchor.Centre,
                Position = origin,
                Size = new Vector2(radius * 2),
                Scale = new Vector2(0.2f),
                Alpha = StateOpacity.Pressed,
            });

            activeRipple.ScaleTo(1, Motion.Medium1, Motion.Standard);
            activeRipple.MoveTo(DrawSize / 2, Motion.Medium1, Motion.Standard);
            rippleStart = Time.Current;
        }

        /// <summary>Fades the current ripple once it has had time to spread.</summary>
        public void Release()
        {
            if (activeRipple == null)
                return;

            var ripple = activeRipple;
            activeRipple = null;

            double wait = Math.Max(0, rippleStart + Motion.Short4 - Time.Current);
            ripple.Delay(wait).FadeOut(Motion.Short3, Motion.Linear).Expire();
        }

        /// <summary>A short ripple, for a click that ends as it starts (a key press, or a click from code).</summary>
        public void Flash(Vector2? screenSpacePosition = null)
        {
            if (screenSpacePosition is Vector2 position)
                Press(position);
            else
                PressCentre();

            Release();
        }

        private void update()
        {
            float alpha = !enabled ? 0 : dragged ? StateOpacity.Dragged : focused ? StateOpacity.Focus : hovered ? StateOpacity.Hover : 0;
            layer.FadeTo(alpha, Motion.Short3, Motion.Standard);
        }
    }
}
