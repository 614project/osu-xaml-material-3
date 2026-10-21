using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// A rounded outline that fills its parent and can leave a gap in its top edge, where an outlined text field's floating label
    /// sits. The gap runs from <see cref="NotchStart"/> to <see cref="NotchEnd"/> (in pixels from the left); its colour is the
    /// drawable's Colour.
    /// </summary>
    public partial class NotchedOutline : FigureShape
    {
        private const int arc_segments = 8;

        private float radius;
        private float notchStart;
        private float notchEnd;
        private Vector2 builtSize;

        public NotchedOutline()
        {
            RelativeSizeAxes = Axes.Both;
            Stroke = Colour4.White;
            StrokeThickness = 1;
            StrokeLineJoin = PenLineJoin.Round;
        }

        /// <summary>The radius of the outer edge.</summary>
        public float ShapeRadius
        {
            get => radius;
            set => set(ref radius, value);
        }

        public float NotchStart
        {
            get => notchStart;
            set => set(ref notchStart, value);
        }

        public float NotchEnd
        {
            get => notchEnd;
            set => set(ref notchEnd, value);
        }

        public float Thickness
        {
            get => StrokeThickness;
            set
            {
                if (StrokeThickness == value)
                    return;

                StrokeThickness = value;
                InvalidateGeometry();
            }
        }

        private void set(ref float field, float value)
        {
            if (field == value)
                return;

            field = value;
            InvalidateGeometry();
        }

        protected override void Update()
        {
            base.Update();

            if (DrawSize != builtSize)
            {
                builtSize = DrawSize;
                InvalidateGeometry();
            }
        }

        protected override FillRule FigureFillRule => FillRule.Nonzero;

        protected override IReadOnlyList<ShapeFigure> GetFigures()
        {
            float t = StrokeThickness;
            float left = t / 2, top = t / 2, right = DrawWidth - t / 2, bottom = DrawHeight - t / 2;

            if (right <= left || bottom <= top)
                return Array.Empty<ShapeFigure>();

            float r = Math.Clamp(radius - t / 2, 0, Math.Min(right - left, bottom - top) / 2);
            bool notched = notchEnd > notchStart;

            float gapStart = Math.Clamp(notchStart, left + r, right - r);
            float gapEnd = Math.Clamp(notchEnd, left + r, right - r);

            var points = new List<Vector2>();

            // Clockwise from the end of the gap (or the top-left corner's end), round to its start.
            points.Add(new Vector2(notched ? gapEnd : left + r, top));
            arc(points, new Vector2(right - r, top + r), r, -90);
            arc(points, new Vector2(right - r, bottom - r), r, 0);
            arc(points, new Vector2(left + r, bottom - r), r, 90);
            arc(points, new Vector2(left + r, top + r), r, 180);

            if (notched)
                points.Add(new Vector2(gapStart, top));

            return new[] { new ShapeFigure(points, !notched, false) };
        }

        private static void arc(List<Vector2> points, Vector2 centre, float r, float startDegrees)
        {
            if (r <= 0)
            {
                points.Add(centre);
                return;
            }

            for (int i = 0; i <= arc_segments; i++)
            {
                float a = MathF.PI / 180 * (startDegrees + 90f * i / arc_segments);
                points.Add(centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r);
            }
        }

        public override Vector2 Measure(Vector2 availableSize) => Vector2.Zero;
    }
}
