using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

[assembly: OsuXaml.XamlGlobalProperties(typeof(OsuXaml.Material3.Tooltips))]

namespace OsuXaml.Material3
{
    /// <summary>
    /// The <c>PlainTooltip</c> property every XAML element accepts: <c>&lt;m3:IconButton Icon="Edit" PlainTooltip="Edit" /&gt;</c> shows
    /// Material's plain tooltip centred below the element while it is hovered. It goes through osu-xaml's ToolTipService, so the
    /// delays and hiding rules are the same as for <c>ToolTip</c>; for a rich tooltip, set a <see cref="RichTooltip"/> as the ToolTip.
    /// </summary>
    public static class Tooltips
    {
        public static void SetPlainTooltip(Drawable element, LocalisableString text)
        {
            if (string.IsNullOrEmpty(text.ToString()))
            {
                ToolTipService.SetToolTip(element, (Drawable)null);
                return;
            }

            if (ToolTipService.GetToolTip(element) is PlainTooltip existing)
            {
                existing.Text = text;
                return;
            }

            ToolTipService.SetToolTip(element, new PlainTooltip { Text = text, Target = element });
            ToolTipService.SetPlacement(element, PlacementMode.Bottom);
        }

        public static void SetPlainTooltip(Drawable element, string text) => SetPlainTooltip(element, (LocalisableString)(text ?? string.Empty));

        public static LocalisableString GetPlainTooltip(Drawable element) => ToolTipService.GetToolTip(element) is PlainTooltip tooltip ? tooltip.Text : default;
    }

    /// <summary>
    /// What Material's tooltips share: osu-xaml's tooltip with Material's colours, taken from the theme of the element it is for
    /// (<see cref="Target"/>).
    /// </summary>
    public abstract partial class MaterialTooltip : ToolTip
    {
        private ColorScheme applied;

        protected MaterialTooltip()
        {
            BorderThickness = 0;
            Chrome.EdgeEffect = default(EdgeEffectParameters);
        }

        /// <summary>The element the tooltip describes; its theme colours the tooltip.</summary>
        public Drawable Target { get; set; }

        protected override void Update()
        {
            base.Update();

            var scheme = MaterialTheme.Of(Target).Scheme.Value;

            if (scheme != applied)
            {
                applied = scheme;
                ApplyColours(scheme);
            }
        }

        protected abstract void ApplyColours(ColorScheme scheme);
    }

    /// <summary>
    /// Material's plain tooltip: a short label in a small InverseSurface box. Usually set with the <c>PlainTooltip</c> property;
    /// it centres itself under its <see cref="MaterialTooltip.Target"/> when placed at the bottom.
    /// </summary>
    public partial class PlainTooltip : MaterialTooltip
    {
        private readonly Text label;

        public PlainTooltip()
        {
            CornerRadius = 4;
            Padding = new MarginPadding { Horizontal = 8, Vertical = 4 };
            Content = label = new Text(TypeStyle.BodySmall) { ColorRole = null };
        }

        public LocalisableString Text
        {
            get => label.Text;
            set => label.Text = value;
        }

        protected override void ApplyColours(ColorScheme scheme)
        {
            Background = scheme.InverseSurface;
            label.Colour = scheme.InverseOnSurface;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (Target == null || Parent == null || ToolTipService.GetPlacement(Target) != PlacementMode.Bottom)
                return;

            // osu-xaml places a bottom tooltip at the element's left edge; Material centres it.
            var quad = Target.ScreenSpaceDrawQuad;
            float targetWidth = Parent.ToLocalSpace(quad.TopRight).X - Parent.ToLocalSpace(quad.TopLeft).X;
            Chrome.X = (targetWidth - DrawWidth) / 2;
        }
    }

    /// <summary>
    /// Material's rich tooltip: a subhead and supporting text on a 12dp rounded SurfaceContainer card with a shadow.
    /// <code>
    /// &lt;m3:IconButton Icon="Info"&gt;
    ///   &lt;ToolTipService.ToolTip&gt;&lt;m3:RichTooltip Subhead="Backups" SupportingText="Saved every night." /&gt;&lt;/ToolTipService.ToolTip&gt;
    /// &lt;/m3:IconButton&gt;
    /// </code>
    /// </summary>
    public partial class RichTooltip : MaterialTooltip
    {
        private readonly StackPanel layout;
        private readonly Text subhead;
        private readonly Text supporting;
        private readonly ElevationShadow shadow;

        public RichTooltip()
        {
            CornerRadius = 12;
            Padding = new MarginPadding { Horizontal = 16, Top = 12, Bottom = 12 };

            AddInternal(shadow = new ElevationShadow { Depth = 1, ShapeRadius = 12, Level = Elevation.Level2 });

            Content = layout = new StackPanel { Spacing = 4 };
            layout.Add(subhead = new Text(TypeStyle.TitleSmall) { ColorRole = null });
            layout.Add(supporting = new Text(TypeStyle.BodyMedium) { ColorRole = null, TextWrapping = TextWrapping.Wrap });
        }

        public LocalisableString Subhead
        {
            get => subhead.Text;
            set
            {
                subhead.Text = value;
                Layout.SetVisibility(subhead, string.IsNullOrEmpty(value.ToString()) ? Visibility.Collapsed : Visibility.Visible);
            }
        }

        public LocalisableString SupportingText
        {
            get => supporting.Text;
            set => supporting.Text = value;
        }

        // Rich tooltips wrap their text at 320dp wide.
        public override Vector2 Measure(Vector2 availableSize) => base.Measure(Vector2.ComponentMin(availableSize, new Vector2(320, float.PositiveInfinity)));

        protected override void ApplyColours(ColorScheme scheme)
        {
            Background = scheme.SurfaceContainer;
            subhead.Colour = scheme.OnSurfaceVariant;
            supporting.Colour = scheme.OnSurfaceVariant;
            shadow.ShadowColour = scheme.Shadow;
        }
    }
}
