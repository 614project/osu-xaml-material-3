using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's badge: a 6dp dot in the error colour, or with <see cref="Text"/>, a 16dp pill with a short count or label.
    /// <c>&lt;m3:Badge Text="3" /&gt;</c>. Navigation items have badges built in (<c>BadgeText</c>, <c>ShowBadge</c>).
    /// </summary>
    public partial class Badge : MaterialControl
    {
        private const float small = 6;
        private const float large = 16;

        private readonly Circle fill;
        private readonly Text label;

        public Badge()
        {
            InternalChildren = new Drawable[]
            {
                fill = new Circle { RelativeSizeAxes = Axes.Both },
                label = new Text(TypeStyle.LabelSmall) { ColorRole = null, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0 },
            };
        }

        /// <summary>The count or label; empty (the default) shows a small dot.</summary>
        public LocalisableString Text
        {
            get => label.Text;
            set
            {
                label.Text = value;
                label.Alpha = isLarge ? 1 : 0;
                InvalidateMeasure();
            }
        }

        private bool isLarge => !string.IsNullOrEmpty(label.Text.ToString());

        /// <summary>The size the badge takes for its text.</summary>
        internal Vector2 DesiredSize => isLarge ? new Vector2(Math.Max(large, label.DrawWidth + 8), large) : new Vector2(small);

        protected override Vector2 MeasureOverride(Vector2 availableSize) => DesiredSize;

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
            SetColour(fill, Scheme.Error, animate);
            SetColour(label, Scheme.OnError, animate);
        }

        /// <summary>Places a badge on an icon the way Material does: a dot on its top-right corner, a pill spreading right from there.</summary>
        internal static void PlaceOn(Badge badge, Drawable icon)
        {
            // Both share a parent; the badge is placed against the icon's box there.
            var box = icon.BoundingBox;
            badge.Anchor = Anchor.TopLeft;

            if (badge.isLarge)
            {
                badge.Origin = Anchor.BottomLeft;
                badge.Position = new Vector2(box.Left + 12, box.Top + 14);
            }
            else
            {
                badge.Origin = Anchor.TopLeft;
                badge.Position = new Vector2(box.Right - small, box.Top);
            }
        }
    }
}
