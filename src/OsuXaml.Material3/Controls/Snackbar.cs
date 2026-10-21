using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using OsuXaml.Controls;
using osuTK;

namespace OsuXaml.Material3
{
    /// <summary>How long a snackbar stays.</summary>
    public enum SnackbarDuration
    {
        /// <summary>4 seconds.</summary>
        Short,

        /// <summary>10 seconds.</summary>
        Long,

        /// <summary>Until its action or close button is clicked.</summary>
        Indefinite,
    }

    /// <summary>
    /// Shows Material's snackbars (brief messages at the bottom, with an optional action) over its content. Wrap the screen in one:
    /// <c>&lt;m3:SnackbarHost x:Name="snackbars"&gt;&lt;Grid&gt;...&lt;/Grid&gt;&lt;/m3:SnackbarHost&gt;</c>, then call
    /// <see cref="Show"/>, from code inside it through <see cref="Find"/>. Snackbars queue and show one at a time.
    /// </summary>
    public partial class SnackbarHost : ContentControl
    {
        private readonly Container layer;
        private readonly Queue<Snackbar> queue = new Queue<Snackbar>();
        private Snackbar current;
        private double shownAt;

        public SnackbarHost()
        {
            AddInternal(layer = new Container { RelativeSizeAxes = Axes.Both, Depth = -1, Padding = new MarginPadding(16) });
        }

        /// <summary>The snackbar host <paramref name="drawable"/> is in, or null.</summary>
        public static SnackbarHost Find(Drawable drawable)
        {
            for (var d = drawable; d != null; d = d.Parent)
            {
                if (d is SnackbarHost host)
                    return host;
            }

            return null;
        }

        /// <summary>
        /// Shows a message, after any already showing. With <paramref name="actionText"/> it has a button that runs
        /// <paramref name="action"/> and dismisses it; <paramref name="showClose"/> adds a close button.
        /// </summary>
        public Snackbar Show(LocalisableString message, LocalisableString actionText = default, Action action = null, bool showClose = false,
                             SnackbarDuration duration = SnackbarDuration.Short)
        {
            var snackbar = new Snackbar(this)
            {
                Message = message,
                ActionText = actionText,
                ShowClose = showClose,
                Duration = duration,
            };

            if (action != null)
                snackbar.ActionClicked += action;

            Schedule(() =>
            {
                queue.Enqueue(snackbar);
                showNext();
            });

            return snackbar;
        }

        /// <summary>Dismisses the snackbar showing now, if any.</summary>
        public void DismissCurrent() => Schedule(() => dismiss(current));

        internal void Dismiss(Snackbar snackbar) => Schedule(() => dismiss(snackbar));

        private void dismiss(Snackbar snackbar)
        {
            if (snackbar == null || snackbar != current)
                return;

            current = null;
            snackbar.FadeOut(Motion.Short2, Motion.StandardAccelerate).Expire();
            Scheduler.AddDelayed(showNext, Motion.Short2);
        }

        private void showNext()
        {
            if (current != null || queue.Count == 0)
                return;

            current = queue.Dequeue();
            shownAt = Time.Current;

            layer.Add(current);
            current.Alpha = 0;
            current.FadeIn(Motion.Short4, Motion.StandardDecelerate);
            current.MoveToY(8).MoveToY(0, Motion.Medium2, Motion.EmphasizedDecelerate);
        }

        protected override void Update()
        {
            base.Update();

            if (current == null)
                return;

            double duration = current.Duration switch
            {
                SnackbarDuration.Short => 4000,
                SnackbarDuration.Long => 10000,
                _ => double.PositiveInfinity,
            };

            if (Time.Current - shownAt >= duration)
                dismiss(current);
        }
    }

    /// <summary>
    /// A snackbar: one or two lines of text on InverseSurface, an optional action in InversePrimary and an optional close button.
    /// Made by <see cref="SnackbarHost.Show"/>.
    /// </summary>
    public partial class Snackbar : MaterialControl
    {
        private const float max_width = 600;

        private readonly SnackbarHost host;
        private readonly ElevationShadow shadow;
        private readonly Container clip;
        private readonly Box background;
        private readonly Text message;
        private readonly SnackbarActionButton actionButton;
        private readonly IconButton closeButton;

        internal Snackbar(SnackbarHost host)
        {
            this.host = host;

            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            // Sized in Update against the host, not to its content alone.
            Size = Vector2.One;

            InternalChildren = new Drawable[]
            {
                shadow = new ElevationShadow { ShapeRadius = 4 },
                clip = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 4,
                    Child = background = new Box { RelativeSizeAxes = Axes.Both },
                },
                message = new Text(TypeStyle.BodyMedium) { ColorRole = null, TextWrapping = TextWrapping.Wrap, X = 16 },
                actionButton = new SnackbarActionButton { Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, Alpha = 0 },
                closeButton = new IconButton { Icon = Symbol.Close, Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, X = -4, Alpha = 0 },
            };

            actionButton.Click += () =>
            {
                ActionClicked?.Invoke();
                host.Dismiss(this);
            };

            closeButton.Click += () => host.Dismiss(this);
        }

        public LocalisableString Message
        {
            get => message.Text;
            set => message.Text = value;
        }

        public LocalisableString ActionText
        {
            get => actionButton.Text;
            set
            {
                actionButton.Text = value;
                actionButton.Alpha = string.IsNullOrEmpty(value.ToString()) ? 0 : 1;
            }
        }

        public bool ShowClose
        {
            get => closeButton.Alpha > 0;
            set => closeButton.Alpha = value ? 1 : 0;
        }

        public SnackbarDuration Duration { get; set; }

        /// <summary>Raised when the action is clicked.</summary>
        public event Action ActionClicked;

        /// <summary>Dismisses this snackbar.</summary>
        public void Dismiss() => host.Dismiss(this);

        private float trailingWidth
        {
            get
            {
                float w = 0;
                if (closeButton.Alpha > 0) w += 4 + closeButton.DrawWidth;
                if (actionButton.Alpha > 0) w += (closeButton.Alpha > 0 ? 0 : 8) + actionButton.DrawWidth;
                return w;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            float available = Math.Min(max_width, float.IsFinite(availableSize.X) ? availableSize.X : max_width);
            float textMax = Math.Max(1, available - 16 - trailingWidth - (trailingWidth > 0 ? 8 : 16));

            message.Measure(new Vector2(textMax, float.PositiveInfinity));

            float width = 16 + message.DrawWidth + (trailingWidth > 0 ? 8 + trailingWidth : 16);
            return new Vector2(Math.Min(available, Math.Max(width, 200)), Math.Max(48, message.DrawHeight + 28));
        }

        protected override void Update()
        {
            base.Update();

            // A snackbar sizes itself to its text, within the host's width less its margins.
            var size = MeasureOverride(Parent?.ChildSize ?? new Vector2(max_width));
            Size = size;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            message.Y = (DrawHeight - message.DrawHeight) / 2;
            actionButton.X = closeButton.Alpha > 0 ? -(4 + closeButton.DrawWidth) : -8;
        }

        protected override bool Interactive => false;

        protected override void UpdateColors(bool animate)
        {
            SetColour(background, Scheme.InverseSurface, animate);
            SetColour(message, Scheme.InverseOnSurface, animate);
            closeButton.IconColor = Scheme.InverseOnSurface;
            shadow.ShadowColour = Scheme.Shadow;
            shadow.Level = Elevation.Level3;
        }

        private partial class SnackbarActionButton : TextButton
        {
            protected override (ColorRole? Container, ColorRole Content, ColorRole? Outline, int Level, int HoverLevel) Style
                => (null, ColorRole.InversePrimary, null, 0, 0);
        }
    }
}
