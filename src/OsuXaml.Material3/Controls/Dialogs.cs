using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>
    /// Material's basic dialog, built on osu-xaml's modal <see cref="Dialog"/>: an optional icon, a headline, supporting text, any
    /// content, and actions at the bottom end, in a 28dp rounded SurfaceContainerHigh container.
    /// <code>
    /// &lt;m3:AlertDialog x:Class="App.DeleteDialog" Icon="Delete" Headline="Delete draft?" SupportingText="This can't be undone."&gt;
    ///   &lt;m3:AlertDialog.Actions&gt;
    ///     &lt;m3:TextButton Text="Cancel" IsCancel="True" /&gt;&lt;m3:TextButton Text="Delete" IsDefault="True" Click="Confirm" /&gt;
    ///   &lt;/m3:AlertDialog.Actions&gt;
    /// &lt;/m3:AlertDialog&gt;
    /// </code>
    /// Show it with <c>await dialog.ShowDialog(owner)</c> as any osu-xaml dialog, or use <see cref="Show"/> for a quick question.
    /// It takes the theme of the element it is shown from. Enter clicks the IsDefault button and Escape the IsCancel one.
    /// </summary>
    public partial class AlertDialog : Dialog
    {
        private readonly StackPanel layout;
        private readonly Icon icon;
        private readonly Text headline;
        private readonly Text supporting;
        private readonly ContentPresenter body;
        private readonly StackPanel actionRow;
        private readonly ElevationShadow shadow;

        private MaterialTheme theme;
        private IBindable<ColorScheme> scheme;

        public AlertDialog()
        {
            Title = null;
            ShowCloseButton = false;
            BorderThickness = 0;
            CornerRadius = 28;
            Padding = new MarginPadding(24);
            MinWidth = 280;
            MaxWidth = 560;
            Chrome.EdgeEffect = default(EdgeEffectParameters);

            AddInternal(shadow = new ElevationShadow { Depth = 1 });

            Content = layout = new StackPanel();

            layout.Add(icon = new Icon { ColorRole = Material3.ColorRole.Secondary, Margin = new MarginPadding { Bottom = 16 } });
            layout.Add(headline = new Text(TypeStyle.HeadlineSmall) { TextWrapping = TextWrapping.Wrap, Margin = new MarginPadding { Bottom = 16 } });
            layout.Add(supporting = new Text(TypeStyle.BodyMedium) { ColorRole = Material3.ColorRole.OnSurfaceVariant, TextWrapping = TextWrapping.Wrap });
            layout.Add(body = new ContentPresenter());
            layout.Add(actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new MarginPadding { Top = 24 } });

            Layout.SetHorizontalAlignment(icon, HorizontalAlignment.Center);
            Layout.SetHorizontalAlignment(actionRow, HorizontalAlignment.Right);

            Actions = new DialogActions(actionRow);
            apply();
        }

        /// <summary>An icon above the headline; with one, the headline is centred.</summary>
        public Symbol Icon
        {
            get => icon.Symbol;
            set
            {
                icon.Symbol = value;
                apply();
            }
        }

        public LocalisableString Headline
        {
            get => headline.Text;
            set
            {
                headline.Text = value;
                apply();
            }
        }

        public LocalisableString SupportingText
        {
            get => supporting.Text;
            set
            {
                supporting.Text = value;
                apply();
            }
        }

        /// <summary>Content below the supporting text (a list, a form); XAML child elements land here.</summary>
        public Drawable Body
        {
            get => body.Content as Drawable;
            set
            {
                body.Content = value;
                apply();
            }
        }

        public void Add(Drawable content) => Body = content;

        /// <summary>The buttons at the bottom end, usually <see cref="TextButton"/>s.</summary>
        public DialogActions Actions { get; }

        /// <summary>The theme the dialog draws in; by default that of the element it is shown from.</summary>
        public MaterialTheme Theme
        {
            get => theme;
            set => theme = value;
        }

        private void apply()
        {
            bool hasIcon = icon.Symbol != Symbol.None;

            Layout.SetVisibility(icon, hasIcon ? Visibility.Visible : Visibility.Collapsed);
            Layout.SetVisibility(headline, string.IsNullOrEmpty(headline.Text.ToString()) ? Visibility.Collapsed : Visibility.Visible);
            Layout.SetVisibility(supporting, string.IsNullOrEmpty(supporting.Text.ToString()) ? Visibility.Collapsed : Visibility.Visible);
            Layout.SetVisibility(body, body.Content == null ? Visibility.Collapsed : Visibility.Visible);

            headline.TextAlignment = hasIcon ? TextAlignment.Center : TextAlignment.Left;
            Layout.SetHorizontalAlignment(headline, hasIcon ? HorizontalAlignment.Center : HorizontalAlignment.Stretch);
            body.Margin = new MarginPadding { Top = Layout.IsCollapsed(supporting) ? 0 : 16 };
        }

        /// <summary>Shows the dialog over the window of <paramref name="owner"/>, in its theme.</summary>
        public new Task<bool?> ShowDialog(Drawable owner)
        {
            theme ??= MaterialTheme.Of(owner);
            return base.ShowDialog(owner);
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            theme ??= parent.Get<MaterialTheme>() ?? MaterialTheme.Of(Owner);

            var dependencies = new DependencyContainer(parent);
            dependencies.Cache(theme);
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            scheme = theme.Scheme.GetBoundCopy();
            scheme.BindValueChanged(e =>
            {
                Background = e.NewValue.SurfaceContainerHigh;
                shadow.ShadowColour = e.NewValue.Shadow;
            }, true);

            shadow.Level = Elevation.Level3;
        }

        protected override void Update()
        {
            base.Update();
            shadow.ShapeRadius = CornerRadius;
        }

        /// <summary>
        /// Asks a question in a dialog over the window of <paramref name="owner"/>: true when confirmed, false when dismissed.
        /// </summary>
        public static Task<bool?> Show(Drawable owner, LocalisableString headline, LocalisableString supportingText, LocalisableString confirmText,
                                       LocalisableString dismissText = default, Symbol icon = Symbol.None)
        {
            var dialog = new AlertDialog { Headline = headline, SupportingText = supportingText, Icon = icon };

            if (!string.IsNullOrEmpty(dismissText.ToString()))
            {
                var dismiss = new TextButton { Text = dismissText };
                DialogService.SetIsCancel(dismiss, true);
                dismiss.Click += () => dialog.DialogResult = false;
                dialog.Actions.Add(dismiss);
            }

            var confirm = new TextButton { Text = confirmText };
            DialogService.SetIsDefault(confirm, true);
            confirm.Click += () => dialog.DialogResult = true;
            dialog.Actions.Add(confirm);

            return dialog.ShowDialog(owner);
        }
    }

    /// <summary>The action buttons of an <see cref="AlertDialog"/>.</summary>
    public sealed class DialogActions
    {
        private readonly StackPanel row;

        internal DialogActions(StackPanel row)
        {
            this.row = row;
        }

        public void Add(Drawable action) => row.Add(action);
    }
}
