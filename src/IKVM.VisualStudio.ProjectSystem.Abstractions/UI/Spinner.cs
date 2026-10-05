using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Shows that something is in progress: an arc of the foreground color turning on a faint ring, only while visible.
/// </summary>
public sealed class Spinner : FrameworkElement
{

    /// <summary>
    /// The color of the arc, by default the text color around it.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(Spinner), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// The width of the ring.
    /// </summary>
    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(Spinner), new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

    static readonly Duration Turn = new Duration(TimeSpan.FromSeconds(0.9));

    readonly RotateTransform _rotation = new RotateTransform();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public Spinner()
    {
        Width = 16;
        Height = 16;
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = _rotation;
        IsHitTestVisible = false;
        IsVisibleChanged += (s, e) => Animate((bool)e.NewValue);
    }

    /// <inheritdoc cref="ForegroundProperty" />
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <inheritdoc cref="ThicknessProperty" />
    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>
    /// Turns the arc while the spinner is visible, and stops it otherwise, so that hidden spinners cost nothing.
    /// </summary>
    void Animate(bool visible)
    {
        if (visible && SystemParameters.ClientAreaAnimation)
            _rotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, Turn) { RepeatBehavior = RepeatBehavior.Forever });
        else
            _rotation.BeginAnimation(RotateTransform.AngleProperty, null);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
            return;

        var thickness = Math.Min(Thickness, size / 4);
        var radius = (size - thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);

        // the faint ring
        var ring = Foreground.CloneCurrentValue();
        ring.Opacity = 0.2;
        drawingContext.DrawEllipse(null, new Pen(ring, thickness), center, radius, radius);

        // a quarter of it, which turns
        var arc = new StreamGeometry();
        using (var context = arc.Open())
        {
            context.BeginFigure(new Point(center.X, center.Y - radius), false, false);
            context.ArcTo(new Point(center.X + radius, center.Y), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
        }

        arc.Freeze();
        drawingContext.DrawGeometry(null, new Pen(Foreground, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
    }

}
