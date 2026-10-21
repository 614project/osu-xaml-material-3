using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OsuXaml.Controls;
using OsuXaml.Material3;
using osuTK;

namespace Material3Gallery
{
    public partial class ThemePage : UserControl
    {
        // Each row of swatches: a role and the role its label is drawn in.
        private static readonly (ColorRole Role, ColorRole Text)[][] rows =
        {
            new[]
            {
                (ColorRole.Primary, ColorRole.OnPrimary), (ColorRole.Secondary, ColorRole.OnSecondary),
                (ColorRole.Tertiary, ColorRole.OnTertiary), (ColorRole.Error, ColorRole.OnError),
            },
            new[]
            {
                (ColorRole.OnPrimary, ColorRole.Primary), (ColorRole.OnSecondary, ColorRole.Secondary),
                (ColorRole.OnTertiary, ColorRole.Tertiary), (ColorRole.OnError, ColorRole.Error),
            },
            new[]
            {
                (ColorRole.PrimaryContainer, ColorRole.OnPrimaryContainer), (ColorRole.SecondaryContainer, ColorRole.OnSecondaryContainer),
                (ColorRole.TertiaryContainer, ColorRole.OnTertiaryContainer), (ColorRole.ErrorContainer, ColorRole.OnErrorContainer),
            },
            new[]
            {
                (ColorRole.OnPrimaryContainer, ColorRole.PrimaryContainer), (ColorRole.OnSecondaryContainer, ColorRole.SecondaryContainer),
                (ColorRole.OnTertiaryContainer, ColorRole.TertiaryContainer), (ColorRole.OnErrorContainer, ColorRole.ErrorContainer),
            },
            new[]
            {
                (ColorRole.PrimaryFixed, ColorRole.OnPrimaryFixed), (ColorRole.PrimaryFixedDim, ColorRole.OnPrimaryFixedVariant),
                (ColorRole.SecondaryFixed, ColorRole.OnSecondaryFixed), (ColorRole.SecondaryFixedDim, ColorRole.OnSecondaryFixedVariant),
                (ColorRole.TertiaryFixed, ColorRole.OnTertiaryFixed), (ColorRole.TertiaryFixedDim, ColorRole.OnTertiaryFixedVariant),
            },
            new[]
            {
                (ColorRole.SurfaceDim, ColorRole.OnSurface), (ColorRole.Surface, ColorRole.OnSurface), (ColorRole.SurfaceBright, ColorRole.OnSurface),
                (ColorRole.InverseSurface, ColorRole.InverseOnSurface), (ColorRole.InverseOnSurface, ColorRole.InverseSurface),
                (ColorRole.InversePrimary, ColorRole.InverseSurface),
            },
            new[]
            {
                (ColorRole.SurfaceContainerLowest, ColorRole.OnSurface), (ColorRole.SurfaceContainerLow, ColorRole.OnSurface),
                (ColorRole.SurfaceContainer, ColorRole.OnSurface), (ColorRole.SurfaceContainerHigh, ColorRole.OnSurface),
                (ColorRole.SurfaceContainerHighest, ColorRole.OnSurface),
            },
            new[]
            {
                (ColorRole.OnSurface, ColorRole.Surface), (ColorRole.OnSurfaceVariant, ColorRole.Surface), (ColorRole.Outline, ColorRole.Surface),
                (ColorRole.OutlineVariant, ColorRole.OnSurface), (ColorRole.Scrim, ColorRole.InverseOnSurface), (ColorRole.Shadow, ColorRole.InverseOnSurface),
            },
        };

        [BackgroundDependencyLoader]
        private void load(GalleryViewModel viewModel)
        {
            InitializeComponent(viewModel);

            foreach (var row in rows)
            {
                var grid = new Grid { ColumnSpacing = 8 };

                for (int i = 0; i < row.Length; i++)
                {
                    grid.ColumnDefinitions.Add(GridLength.Star);

                    var swatch = new Swatch(row[i].Role, row[i].Text);
                    Grid.SetColumn(swatch, i);
                    grid.Add(swatch);
                }

                swatches.Add(grid);
            }
        }

        /// <summary>A colour role's swatch, labelled with its name and hex value.</summary>
        private partial class Swatch : MaterialControl
        {
            private readonly ColorRole role;
            private readonly ColorRole textRole;
            private readonly Container clip;
            private readonly Box fill;
            private readonly Text name;
            private readonly Text hex;

            public Swatch(ColorRole role, ColorRole textRole)
            {
                this.role = role;
                this.textRole = textRole;

                InternalChild = clip = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 8,
                    BorderThickness = 1,
                    Children = new Drawable[]
                    {
                        fill = new Box { RelativeSizeAxes = Axes.Both },
                        name = new Text(TypeStyle.LabelMedium) { ColorRole = null, Text = role.ToString(), Position = new Vector2(10, 8) },
                        hex = new Text(TypeStyle.BodySmall) { ColorRole = null, Position = new Vector2(10, 38) },
                    },
                };
            }

            protected override bool Interactive => false;

            protected override Vector2 MeasureOverride(Vector2 availableSize) => new Vector2(120, 64);

            protected override void UpdateColors(bool animate)
            {
                var colour = Scheme[role];

                SetColour(fill, colour, animate);
                SetColour(name, Scheme[textRole], animate);
                SetColour(hex, Scheme[textRole], animate);
                clip.BorderColour = Scheme.OutlineVariant;
                hex.Text = "#" + colour.ToHex().Substring(1, 6);
            }
        }
    }
}
