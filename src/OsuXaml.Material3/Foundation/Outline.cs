using System;
using osu.Framework.Graphics;
using OsuXaml.Controls;

namespace OsuXaml.Material3
{
    /// <summary>
    /// A rounded outline that fills its parent, drawn as an antialiased stroke (osu-xaml's Rectangle shape) rather than a masking
    /// border, which fades in and out along curves at a 1dp width. Its colour is the drawable's Colour.
    /// </summary>
    public partial class Outline : Rectangle
    {
        private float cornerRadius;

        public Outline()
        {
            RelativeSizeAxes = Axes.Both;
            Stroke = Colour4.White;
            StrokeThickness = 1;
        }

        /// <summary>The radius of the outer edge, matching the corner radius of the shape it outlines.</summary>
        public float ShapeRadius
        {
            get => cornerRadius;
            set
            {
                if (cornerRadius == value)
                    return;

                cornerRadius = value;
                applyRadius();
            }
        }

        public float Thickness
        {
            get => StrokeThickness;
            set
            {
                StrokeThickness = value;
                applyRadius();
            }
        }

        // The stroke's centre line is half its width inside the bounds, so its radius is that much smaller.
        private void applyRadius()
        {
            float radius = Math.Max(0, cornerRadius - StrokeThickness / 2);
            RadiusX = radius;
            RadiusY = radius;
        }
    }
}
