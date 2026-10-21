using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// The two shadows of a Material elevation level (a key light below and an ambient light around), cast by invisible shapes
    /// the size of the component. It sits behind the component's container and fills its parent.
    /// </summary>
    /// <remarks>
    /// A CSS box-shadow blurs the shape, so at the shape's edge it is at half strength; an osu!framework edge effect is at full
    /// strength there and fades out over its radius. Each caster is inset by half the blur so the two fall off alike.
    /// </remarks>
    public partial class ElevationShadow : CompositeDrawable
    {
        private readonly Container key;
        private readonly Container ambient;

        private int level;
        private float cornerRadius;
        private Colour4 shadowColour = Colour4.Black;

        public ElevationShadow()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                ambient = new Container { Anchor = Anchor.Centre, Origin = Anchor.Centre, Masking = true },
                key = new Container { Anchor = Anchor.Centre, Origin = Anchor.Centre, Masking = true },
            };
        }

        /// <summary>0 to 5.</summary>
        public int Level
        {
            get => level;
            set => SetLevel(value, false);
        }

        public float ShapeRadius
        {
            get => cornerRadius;
            set => cornerRadius = value;
        }

        public Colour4 ShadowColour
        {
            get => shadowColour;
            set
            {
                if (shadowColour == value)
                    return;

                shadowColour = value;
                SetLevel(level, false);
            }
        }

        /// <summary>Changes the level, gliding the shadows there when <paramref name="animate"/> is set.</summary>
        public void SetLevel(int newLevel, bool animate)
        {
            level = Math.Clamp(newLevel, 0, 5);
            double duration = animate ? Motion.Short4 : 0;

            key.TweenEdgeEffectTo(Elevation.KeyShadow(level, shadowColour), duration, Motion.Standard);
            ambient.TweenEdgeEffectTo(Elevation.AmbientShadow(level, shadowColour), duration, Motion.Standard);
        }

        protected override void Update()
        {
            base.Update();

            place(key, Elevation.KeyShadow(level, shadowColour).Radius, 0);
            place(ambient, Elevation.AmbientShadow(level, shadowColour).Radius, Elevation.AmbientSpread(level));
        }

        private void place(Container caster, float blur, float spread)
        {
            float grow = spread - blur / 2;
            caster.Size = Vector2.ComponentMax(Vector2.Zero, DrawSize + new Vector2(grow * 2));
            caster.CornerRadius = Math.Max(0, cornerRadius + grow);
        }
    }
}
