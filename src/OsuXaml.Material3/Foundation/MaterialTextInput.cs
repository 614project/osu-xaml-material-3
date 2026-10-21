using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace OsuXaml.Material3
{
    /// <summary>
    /// The text box inside text fields and search bars: osu-xaml's TextBox in Roboto (body large), without a background of its own,
    /// with a caret and selection in the field's accent colour.
    /// </summary>
    internal partial class MaterialTextInput : OsuXaml.Controls.TextBox
    {
        private static readonly FontUsage font = Typescale.Get(TypeStyle.BodyLarge).Font;

        private MaterialCaret fieldCaret;
        private LocalisableString hint;
        private bool hintVisible = true;

        public MaterialTextInput(bool password, float height)
        {
            if (password)
                InputProperties = new TextInputProperties(TextInputType.Password, false);

            Height = height;
            CornerRadius = 0;
            FontSize = font.Size;
            BackgroundUnfocused = Color4.Transparent;
            BackgroundFocused = Color4.Transparent;
            BackgroundCommit = Color4.Transparent;
            TextContainer.Height = 1;
        }

        /// <summary>The placeholder text, shown while <see cref="HintVisible"/> and the box is empty.</summary>
        public LocalisableString Hint
        {
            get => hint;
            set
            {
                hint = value;
                applyHint();
            }
        }

        public bool HintVisible
        {
            get => hintVisible;
            set
            {
                if (hintVisible == value)
                    return;

                hintVisible = value;
                applyHint();
            }
        }

        private void applyHint() => PlaceholderText = hintVisible ? hint : default;

        /// <summary>A disabled field can't be typed in; a read-only one set by the app stays read-only.</summary>
        public bool ReadOnlyByDisabled
        {
            set
            {
                if (value == disabledReadOnly)
                    return;

                disabledReadOnly = value;
                ReadOnly = value || ownReadOnly;

                if (value && HasFocus)
                    GetContainingFocusManager()?.ChangeFocus(null);
            }
        }

        private bool disabledReadOnly;
        private bool ownReadOnly;

        public new bool IsReadOnly
        {
            get => ownReadOnly;
            set
            {
                ownReadOnly = value;
                ReadOnly = value || disabledReadOnly;
            }
        }

        protected override float LeftRightPadding => 0;

        protected override char MaskCharacter => '•';

        protected override Color4 SelectionColour => fieldCaret?.Accent.Opacity(0.3f) ?? Color4.Transparent;

        protected override Drawable GetDrawableCharacter(char c) => new BasicTextBox.FallingDownContainer
        {
            AutoSizeAxes = Axes.Both,
            Child = new SpriteText { Text = c.ToString(), Font = font.With(size: FontSize) },
        };

        protected override SpriteText CreatePlaceholder() => new BasicTextBox.FadingPlaceholderText
        {
            Font = font,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
        };

        protected override Caret CreateCaret() => fieldCaret = new MaterialCaret();

        public void ApplyColours(Colour4 text, Colour4 placeholderColour, Colour4 accent)
        {
            TextFlow.Colour = text;
            Placeholder.Colour = placeholderColour;
            fieldCaret.Accent = accent;
        }
    }

    /// <summary>A caret in the field's accent colour that blinks, and a selection tinted with it.</summary>
    internal partial class MaterialCaret : Caret
    {
        private readonly Box box;

        public MaterialCaret()
        {
            Colour = Color4.Transparent;
            InternalChild = box = new Box { RelativeSizeAxes = Axes.Both };
        }

        public Colour4 Accent { get; set; } = Colour4.Black;

        public override void Hide() => this.FadeOut(200);

        public override void DisplayAt(Vector2 position, float? selectionWidth)
        {
            ClearTransforms();

            if (selectionWidth is float width)
            {
                this.MoveTo(position, 60, Easing.Out);
                this.ResizeWidthTo(width + 1, 60, Easing.Out);
                Colour = Accent;
                Alpha = 0.3f;
                return;
            }

            this.MoveTo(new Vector2(position.X - 1, position.Y), 60, Easing.Out);
            this.ResizeWidthTo(2, 60, Easing.Out);
            Colour = Accent;
            Alpha = 1;
            this.Delay(500).Loop(500, c => c.FadeTo(0).Then(500).FadeTo(1));
        }
    }
}
