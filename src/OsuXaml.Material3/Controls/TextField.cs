using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace OsuXaml.Material3
{
    /// <summary>The two styles of <see cref="TextField"/>.</summary>
    public enum TextFieldVariant
    {
        /// <summary>A tinted container with a line under it: more emphasis, for forms in dialogs and short forms.</summary>
        Filled,

        /// <summary>An outline around the field: less emphasis, for forms with many fields.</summary>
        Outlined,
    }

    /// <summary>
    /// Material's text field, filled or outlined: <c>&lt;m3:TextField Label="Email" Text="{Bind Email}" LeadingIcon="Mail" /&gt;</c>.
    /// The label rests inside the field and floats above the text once it has focus or text. <see cref="SupportingText"/> sits
    /// below, replaced by <see cref="ErrorText"/> while <see cref="IsError"/>; a <see cref="MaxLength"/> shows a counter.
    /// Single line; Enter commits (see <see cref="Committed"/>).
    /// </summary>
    public partial class TextField : MaterialControl
    {
        private const float field_height = 56;
        private const float padding = 16;
        private const float icon_padding = 12;
        private const float input_height = 24;
        private const float support_gap = 4;

        private readonly Container field;
        private readonly Container containerClip;
        private readonly Box containerFill;
        private readonly Box hoverLayer;
        private readonly Box activeIndicator;
        private readonly NotchedOutline outline;
        private readonly Icon leadingIcon;
        private readonly IconButton trailingButton;
        private readonly Text label;
        private readonly Text prefixText;
        private readonly Text suffixText;
        private readonly Container inputHolder;
        private readonly Text supportingText;
        private readonly Text counterText;

        private MaterialTextInput input;

        private TextFieldVariant variant;
        private bool isError;
        private bool isPassword;
        private LocalisableString labelText;
        private LocalisableString placeholder;
        private LocalisableString supporting;
        private LocalisableString errorText;
        private int maxLength;
        private bool focused;

        // 0 with the label resting in the field, 1 with it floating above the text.
        private float floatProgress;

        public TextField()
        {
            InternalChildren = new Drawable[]
            {
                field = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = field_height,
                    Children = new Drawable[]
                    {
                        // The filled container has 4dp corners on top only: its rounded box reaches below a clip.
                        containerClip = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = field_height + 8,
                                Masking = true,
                                CornerRadius = 4,
                                Children = new Drawable[]
                                {
                                    containerFill = new Box { RelativeSizeAxes = Axes.Both },
                                    hoverLayer = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0 },
                                },
                            },
                        },
                        activeIndicator = new Box { RelativeSizeAxes = Axes.X, Anchor = Anchor.BottomLeft, Origin = Anchor.BottomLeft, Height = 1 },
                        outline = new NotchedOutline { ShapeRadius = 4, Alpha = 0 },
                        leadingIcon = new Icon { ColorRole = null, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, X = icon_padding, Alpha = 0 },
                        trailingButton = new IconButton { Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, X = -(icon_padding - 8), Alpha = 0 },
                        prefixText = new Text(TypeStyle.BodyLarge) { ColorRole = null, Alpha = 0 },
                        suffixText = new Text(TypeStyle.BodyLarge) { ColorRole = null, Alpha = 0 },
                        inputHolder = new Container { Height = input_height },
                        label = new Text(TypeStyle.BodyLarge) { ColorRole = null, Origin = Anchor.CentreLeft, Alpha = 0 },
                    },
                },
                supportingText = new Text(TypeStyle.BodySmall) { ColorRole = null, X = padding, Y = field_height + support_gap, Alpha = 0 },
                counterText = new Text(TypeStyle.BodySmall) { ColorRole = null, Anchor = Anchor.TopRight, Origin = Anchor.TopRight, X = -padding, Y = field_height + support_gap, Alpha = 0 },
            };

            trailingButton.Click += () => TrailingIconClick?.Invoke();

            Text.BindValueChanged(_ =>
            {
                updateCounter();
                OnStateChanged();
            });

            createInput();
        }

        #region Properties

        /// <summary>The text. Binds two-way: <c>Text="{Bind Name}"</c>.</summary>
        public Bindable<string> Text { get; } = new Bindable<string>(string.Empty);

        public TextFieldVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                OnStateChanged();
            }
        }

        /// <summary>The label, resting in the field while it is empty and floating above the text otherwise.</summary>
        public LocalisableString Label
        {
            get => labelText;
            set
            {
                labelText = value;
                label.Text = value;
                label.Alpha = hasLabel ? 1 : 0;
                OnStateChanged();
            }
        }

        /// <summary>A hint shown in the empty field while it has focus (or always, without a label).</summary>
        public LocalisableString Placeholder
        {
            get => placeholder;
            set
            {
                placeholder = value;
                input.Hint = value;
            }
        }

        /// <summary>Help text below the field.</summary>
        public LocalisableString SupportingText
        {
            get => supporting;
            set
            {
                supporting = value;
                updateSupporting();
            }
        }

        /// <summary>Shown in place of <see cref="SupportingText"/> while <see cref="IsError"/>.</summary>
        public LocalisableString ErrorText
        {
            get => errorText;
            set
            {
                errorText = value;
                updateSupporting();
            }
        }

        /// <summary>Draws the field in the error colour, with <see cref="ErrorText"/> below.</summary>
        public bool IsError
        {
            get => isError;
            set
            {
                isError = value;
                trailingButton.Variant = IconButtonVariant.Standard;
                updateSupporting();
                OnStateChanged();
            }
        }

        public Symbol LeadingIcon
        {
            get => leadingIcon.Symbol;
            set
            {
                leadingIcon.Symbol = value;
                leadingIcon.Alpha = value == Symbol.None ? 0 : 1;
            }
        }

        /// <summary>An icon at the end; it becomes a button when <see cref="TrailingIconClick"/> has a handler (a clear button, say).</summary>
        public Symbol TrailingIcon
        {
            get => trailingButton.Icon;
            set
            {
                trailingButton.Icon = value;
                trailingButton.Alpha = value == Symbol.None ? 0 : 1;
            }
        }

        /// <summary>Raised when the trailing icon is clicked.</summary>
        public event Action TrailingIconClick;

        /// <summary>Text before the input, such as a currency sign; shown while the label floats.</summary>
        public LocalisableString Prefix
        {
            get => prefixText.Text;
            set => prefixText.Text = value;
        }

        /// <summary>Text after the input, such as a unit.</summary>
        public LocalisableString Suffix
        {
            get => suffixText.Text;
            set => suffixText.Text = value;
        }

        /// <summary>The longest text that can be entered; above 0 it also shows a counter below the field.</summary>
        public int MaxLength
        {
            get => maxLength;
            set
            {
                maxLength = Math.Max(0, value);
                input.MaxLength = maxLength;
                updateCounter();
            }
        }

        /// <summary>Masks what is typed, as a password box does.</summary>
        public bool IsPassword
        {
            get => isPassword;
            set
            {
                if (isPassword == value)
                    return;

                isPassword = value;
                createInput();
            }
        }

        public bool IsReadOnly
        {
            get => input.IsReadOnly;
            set => input.IsReadOnly = value;
        }

        /// <summary>Raised when Enter is pressed in the field.</summary>
        public event Action<string> Committed;

        /// <summary>Whether the field has keyboard focus.</summary>
        public bool IsFocused => focused;

        private bool showsFocus;

        /// <summary>Draws the field as focused without keyboard focus, as a dropdown does while its menu is open.</summary>
        internal bool ShowsFocus
        {
            get => showsFocus;
            set => showsFocus = value;
        }

        /// <summary>Gives the field keyboard focus.</summary>
        public void Focus() => Schedule(() => GetContainingFocusManager()?.ChangeFocus(input));

        #endregion

        private bool hasLabel => !string.IsNullOrEmpty(labelText.ToString());

        private bool hasText => !string.IsNullOrEmpty(Text.Value);

        private bool shouldFloat => focused || hasText;

        private void createInput()
        {
            string current = Text.Value;

            if (input != null)
            {
                input.Current.UnbindFrom(Text);
                input.OnCommit -= onCommit;
                inputHolder.Remove(input, true);
            }

            input = new MaterialTextInput(isPassword, input_height)
            {
                RelativeSizeAxes = Axes.X,
                Hint = placeholder,
                MaxLength = maxLength,
            };

            inputHolder.Add(input);
            input.Current.Value = current;
            input.Current.BindTo(Text);
            input.OnCommit += onCommit;

            if (IsLoaded)
                UpdateColors(false);
        }

        private void onCommit(osu.Framework.Graphics.UserInterface.TextBox sender, bool newText) => Committed?.Invoke(Text.Value);

        private void updateSupporting()
        {
            var shown = isError && !string.IsNullOrEmpty(errorText.ToString()) ? errorText : supporting;
            supportingText.Text = shown;
            supportingText.Alpha = string.IsNullOrEmpty(shown.ToString()) ? 0 : 1;
            InvalidateMeasure();
        }

        private void updateCounter()
        {
            counterText.Alpha = maxLength > 0 ? 1 : 0;
            counterText.Text = $"{Text.Value?.Length ?? 0}/{maxLength}";
            InvalidateMeasure();
        }

        private bool hasSupportingLine => supportingText.Alpha > 0 || counterText.Alpha > 0;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
            => new Vector2(210, field_height + (hasSupportingLine ? support_gap + 16 : 0));

        #region Input

        public override bool AcceptsFocus => false;

        protected override bool OnClick(ClickEvent e)
        {
            if (IsEnabled && !input.HasFocus)
                GetContainingFocusManager()?.ChangeFocus(input);

            return true;
        }

        protected override void Update()
        {
            base.Update();

            input.ReadOnlyByDisabled = !IsEnabled;

            bool nowFocused = input.HasFocus || showsFocus;

            if (nowFocused != focused)
            {
                focused = nowFocused;
                OnStateChanged();
            }
        }

        protected override void OnStateChanged()
        {
            base.OnStateChanged();

            float target = shouldFloat || !hasLabel ? 1 : 0;

            if (!IsLoaded)
                floatProgress = target;
            else if (target != floatProgress)
                this.TransformTo(nameof(floatProgress), target, Motion.Short4, Motion.Standard);

            // A label resting in the field stands in for the placeholder.
            input.HintVisible = !hasLabel || focused;
        }

        #endregion

        #region Layout and colours

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            bool outlined = variant == TextFieldVariant.Outlined;
            bool hasLeading = leadingIcon.Alpha > 0;
            bool hasTrailing = trailingButton.Alpha > 0;

            float textStart = hasLeading ? icon_padding + 24 + 16 : padding;
            float textEnd = DrawWidth - (hasTrailing ? icon_padding + 24 + 16 : padding);

            // The input sits lower in a filled field with a label, under the floating label.
            float inputTop = !outlined && hasLabel ? 24 : (field_height - input_height) / 2;

            bool showAffixes = floatProgress > 0.5f;
            prefixText.Alpha = showAffixes && !string.IsNullOrEmpty(prefixText.Text.ToString()) ? 1 : 0;
            suffixText.Alpha = showAffixes && !string.IsNullOrEmpty(suffixText.Text.ToString()) ? 1 : 0;

            float x = textStart;

            if (prefixText.Alpha > 0)
            {
                prefixText.Position = new Vector2(x, inputTop);
                x += prefixText.DrawWidth + 2;
            }

            float suffixWidth = suffixText.Alpha > 0 ? suffixText.DrawWidth + 2 : 0;
            suffixText.Position = new Vector2(textEnd - suffixText.DrawWidth, inputTop);

            inputHolder.Position = new Vector2(x, inputTop);
            inputHolder.Width = Math.Max(0, textEnd - suffixWidth - x);

            // The label: body large resting in the middle, body small (75%) floating at the top.
            float scale = 1 - 0.25f * floatProgress;
            float restY = field_height / 2;
            float floatY = outlined ? 0 : 8 + 8;
            float floatX = outlined ? padding : textStart;

            label.Scale = new Vector2(scale);
            label.Position = new Vector2(textStart + (floatX - textStart) * floatProgress, restY + (floatY - restY) * floatProgress);

            if (outlined && hasLabel && floatProgress > 0)
            {
                float width = label.DrawWidth * scale;
                outline.NotchStart = label.X - 4;
                outline.NotchEnd = label.X + (width + 4) * floatProgress + 4 * floatProgress;
            }
            else
            {
                outline.NotchStart = 0;
                outline.NotchEnd = 0;
            }
        }

        protected override void UpdateColors(bool animate)
        {
            bool outlined = variant == TextFieldVariant.Outlined;
            bool hovered = IsHovered && IsEnabled;

            containerClip.Alpha = outlined ? 0 : 1;
            activeIndicator.Alpha = outlined ? 0 : 1;
            outline.Alpha = outlined ? 1 : 0;

            Colour4 accent = isError ? Scheme.Error : Scheme.Primary;
            Colour4 disabled = Scheme.OnSurface.Opacity(StateOpacity.DisabledContent);

            // Container and hover layer (filled only).
            SetColour(containerFill, IsEnabled ? Scheme.SurfaceContainerHighest : Scheme.OnSurface.Opacity(0.04f), animate);
            hoverLayer.Colour = Scheme.OnSurface;
            hoverLayer.FadeTo(hovered && !outlined ? StateOpacity.Hover : 0, Motion.Short3);

            // The line under a filled field, and the outline around an outlined one.
            Colour4 line = !IsEnabled ? disabled
                : focused ? accent
                : isError ? (hovered ? Scheme.OnErrorContainer : Scheme.Error)
                : hovered ? Scheme.OnSurface : Scheme.OnSurfaceVariant;

            SetColour(activeIndicator, line, animate);
            activeIndicator.ResizeHeightTo(focused ? 2 : 1, Motion.Short3);

            Colour4 border = !IsEnabled ? Scheme.OnSurface.Opacity(StateOpacity.DisabledContainer)
                : focused ? accent
                : isError ? (hovered ? Scheme.OnErrorContainer : Scheme.Error)
                : hovered ? Scheme.OnSurface : Scheme.Outline;

            SetColour(outline, border, animate);
            outline.Thickness = focused ? 2 : 1;

            SetColour(label, !IsEnabled ? disabled : focused ? accent : isError ? Scheme.Error : Scheme.OnSurfaceVariant, animate);
            SetColour(leadingIcon, IsEnabled ? Scheme.OnSurfaceVariant : disabled, animate);
            trailingButton.IsEnabled = IsEnabled;
            trailingButton.IconColor = isError && IsEnabled ? Scheme.Error : (Colour4?)null;

            SetColour(prefixText, IsEnabled ? Scheme.OnSurfaceVariant : disabled, animate);
            SetColour(suffixText, IsEnabled ? Scheme.OnSurfaceVariant : disabled, animate);
            SetColour(supportingText, !IsEnabled ? disabled : isError ? Scheme.Error : Scheme.OnSurfaceVariant, animate);
            SetColour(counterText, !IsEnabled ? disabled : Scheme.OnSurfaceVariant, animate);

            input.ApplyColours(IsEnabled ? Scheme.OnSurface : disabled, Scheme.OnSurfaceVariant, accent);
        }

        #endregion
    }
}
